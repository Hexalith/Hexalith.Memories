# Story 10.1: MCP Server & Tool Registration

Status: done

**Effort estimate:** ~7.0-8.0 working days (updated post-review rounds: +0.5 day for Tier-2 tests and upstream-probe; +0.5 day for startup-gate + DI spike + additional security tests). Breakdown:

- **0.5 day — Task 1:** `Hexalith.Memories.Mcp` project scaffold + `Directory.Packages.props` additions + slnx registration + csproj metadata.
- **0.5 day — Task 2:** `MemoriesClient.TraverseAsync` + `GetCaseAsync` + `TraversalRequest` DTO + `MemoriesJsonContext` registration (prerequisite for the `traverse_relations` and `get_case_info` tools).
- **1.0 day (was 0.75) — Task 3:** DAPR service-invocation `HttpClient` wiring + `MemoriesMcpClient` thin wrapper + `Program.cs` composition root + OpenTelemetry passthrough via `ServiceDefaults`. Includes a 30-min DI-shape spike (3.5) and the startup environment gate (3.6) for AC 11.
- **1.25 days — Task 4:** four tool classes (`SearchMemoryTool`, `IngestContentTool`, `TraverseRelationsTool`, `GetCaseInfoTool`) with `[McpServerToolType]` + `[McpServerTool]` + `[Description]` on every parameter (FR58), plus the shared `McpErrorMapper` that translates `MemoriesRemoteException` → `CallToolResult { IsError = true }` with structured `failedService`.
- **0.5 day — Task 5:** AppHost `memories-mcp` `ProjectResource` + its own DAPR sidecar (`app-id: memories-mcp`, `AppPort` omitted for Aspire Testing) + `WaitFor(server)` + Aspire Dashboard resource.
- **0.75 day (was 0.5) — Task 6:** health checks + readiness wiring + mandatory upstream-probe (`MemoriesServerUpstreamHealthCheck` rolling-window 3-strike) + `dapr-sidecar` health check reuse from `ServiceDefaults`.
- **0.75 day — Task 7:** Tier-1 contract tests (tool-schema serialization, FR58 description presence + non-trivial-prose gate, enum `axes` shape, required-parameter gate).
- **1.25 days (was 1.0) — Task 8:** Tier-2 `Hexalith.Memories.Mcp.Tests` — stub `MemoriesClient` via `NSubstitute`, assert every tool's happy path + error-mapping path emits the documented MCP shape, PLUS `MemoriesMcpDaprInvocationHandlerTests` (3 tests — AC 8 guard; may be skipped per Task 3.7 outcome), `McpErrorMapperTests.MapGeneric_DoesNotLeakStackTrace` + `[Theory]`-based `MapGeneric_DoesNotEchoInputValues` + `StructuredContent_ToolField_IsLiteralToolName` security gates, and `McpUnauthenticatedStartupGuardTests` (4 tests — AC 11 guard).
- **0.5 day — Task 9:** Tier-3 Aspire integration test — spin up both services via `DistributedApplicationTestingBuilder`, `McpClient.CreateAsync(HttpClientTransport)` against the MCP endpoint, `ListToolsAsync()` returns the 4 registered tools with typed schemas + descriptions, `CallToolAsync("search_memory", ...)` executes end-to-end across the DAPR hop with a peer-service trace-span assertion proving the sidecar was traversed.
- **0.5 day — Task 10:** docs (including bold unauth warning) + `sprint-status.yaml` + `deferred-work.md` + retro entry.
- **0.5 day cushion** for DAPR service-invocation header surprises (`dapr-app-id` vs. `dapr-app-invocation-target`, sidecar URL resolution under Aspire Testing) and ModelContextProtocol SDK 1.2.0 quirks around `WithHttpTransport(Stateless = true)` + scoped DI.

**HARD prerequisite:** None. Story 10.1 is independently landable against the current `done` Epic 1-8 stack. Epic 9 (`9-1` done, `9-2`/`9-3` ready-for-dev) is parallel-safe — the MCP Server consumes the stable Server REST surface (`/api/search`, `/api/ingest`, `/api/tenants/{tenantId}/traverse`, `/api/tenants/{tenantId}/cases/{caseId}`) that has not changed since Story 7.5. The 9.2/9.3 dual-embedding + handler-registry additions affect *server-internal* behavior but not the Server's public REST contract, so MCP tool responses remain stable. Story 8.5 (`review`) is parallel-safe — the Redis OTEL instrumentation it registers in `ServiceDefaults` is inherited transparently by the MCP Server when it calls `AddServiceDefaults()`.

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** A **separate DAPR-sided C# MCP Server** (`src/Hexalith.Memories.Mcp/`) that exposes exactly four LLM-agent tools — `search_memory`, `ingest_content`, `traverse_relations`, `get_case_info` — each with typed JSON-schema parameters and LLM-consumable descriptions, served via the `ModelContextProtocol.AspNetCore` 1.2.0 Streamable HTTP transport at `/mcp`. The server runs under the Aspire AppHost with **its own DAPR sidecar** (app-id `memories-mcp`) and talks to the Memories Server exclusively via **DAPR service invocation** (no direct Redis, FalkorDB, or secret-store access — NFR11 + ADR D6 isolation). Tools delegate to the existing `MemoriesClient` (`src/Hexalith.Memories.Client.Rest/`) resolved over an `HttpClient` whose base address and `dapr-app-id` header route through the local sidecar at `http://localhost:3500/v1.0/invoke/memories-server/method/`. Errors are mapped in ONE place (`McpErrorMapper.Map`) from the server's existing `ErrorResponse { Code, Message, Suggestion }` envelope (plus a new `failedService` discriminator) to an MCP `CallToolResult { IsError = true, Content = [TextContentBlock] }` so LLM agents receive structured, recovery-oriented text — never raw stack traces. FR54 (4 registered tools), FR58 (typed schemas with descriptions), and NFR20 (MCP protocol conformance) close in this story. **Token-budget shaping, ingress authentication, and degraded-state annotations stay in Story 10.2.**

**What already exists (do NOT rebuild):**

1. **`MemoriesClient` — `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`.** Already carries `SearchAsync(SearchRequest, CancellationToken)`, `HybridSearchAsync(HybridSearchRequest, CancellationToken)`, `IngestAsync(...)`, `ListCasesAsync`, `ListTenantsAsync`, `CreateCaseAsync`, `GetMemoryUnitAsync`, and an `HXL001`-gated `GetTelemetrySummaryAsync`. **Reuse verbatim** via DI in the MCP tool classes. The MCP server package references `Hexalith.Memories.Client.Rest`, **NOT** `Hexalith.Memories.Server` (architecture boundary — MCP delegates to Server via Client per architecture.md §API Boundaries table row "MCP (Phase 1.5)" + §Service Boundaries row "MCP Server (Phase 1.5)"). Adding `TraverseAsync` / `GetCaseAsync` is **Task 2** in this story because the existing `MemoriesClient` surface is missing the two verbs `traverse_relations` / `get_case_info` need — those REST endpoints (`GET /api/tenants/{tenantId}/traverse` at `Program.cs:2773`; `GET /api/tenants/{tenantId}/cases/{caseId}` at `Program.cs:1458`) already exist on the server but have no client method yet.
2. **`SearchRequest` + `HybridSearchRequest` — `src/Hexalith.Memories.Client.Rest/SearchRequest.cs` + `HybridSearchRequest.cs`.** Existing typed request DTOs. **Reuse verbatim.** `SearchMemoryTool` constructs these from MCP tool parameters. `HybridSearchRequest` is used when `axes == "hybrid"`; the single-axis `SearchRequest` covers `syntactic` / `semantic`. The server's graph search path requires `startNodeId`, so graph traversal remains on `traverse_relations` in 10.1.
3. **`ErrorResponse` — `src/Hexalith.Memories.Contracts/V1/ErrorResponse.cs` (`public sealed record ErrorResponse(string Code, string Message, string Suggestion)`).** Already the canonical Server error envelope. `MemoriesRemoteException { StatusCode, Error: ErrorResponse }` carries it across the wire. **Reuse verbatim.** `McpErrorMapper.Map` builds a one-line structured text block from `{Code} [failedService]: {Message}. {Suggestion}` — NOT a JSON dump — because the MCP `TextContentBlock` is the LLM-facing surface and an LLM consumes prose better than nested JSON envelopes.
4. **`MemoriesJsonContext.Options` — `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`.** AOT-safe source-generated `JsonSerializerOptions`. **Reuse verbatim** for every serialize/deserialize call the MCP server makes against Server contract types. Register NEW types introduced by this story (`TraversalRequest`, if added) via `[JsonSerializable(typeof(T))]` per the Story 9.1 Task 2 precedent. **Scope:** use `MemoriesJsonContext.Options` for Server DTO framing; let the ModelContextProtocol SDK handle MCP protocol framing with its own defaults. Do not unify them.
5. **`Hexalith.Memories.ServiceDefaults` — `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`.** Already provides `AddServiceDefaults()` (OpenTelemetry traces + metrics + logs, health checks, service discovery, HTTP resilience) + `MapDefaultEndpoints()` (`/health`, `/alive`, `/ready` with `BackendHealthResponseWriter` JSON output). **Reuse verbatim** via `builder.AddServiceDefaults()` + `app.MapDefaultEndpoints()` in `Hexalith.Memories.Mcp/Program.cs`. The Story 8.5 Redis OTEL wiring is inherited transparently even though the MCP Server does not talk to Redis directly — `ServiceDefaults` skips Redis instrumentation registrations when no keyed `IConnectionMultiplexer` is resolved at `TracerProvider.Build()` time (the `AddRedisKeyedConnectionGuard` helper at `Extensions.cs:161` throws ONLY if a caller has explicitly registered a keyed multiplexer; the MCP Server does not, so the guard is a no-op there). **Task 6 adds the `dapr-sidecar` health check** the same way `Hexalith.Memories.Server/Program.cs:42` does.
6. **AppHost `WithDaprSidecar` + `WaitFor` pattern — `src/Hexalith.Memories.AppHost/Program.cs:67-90`.** Already shows the canonical `AddProject<T>("name").WithDaprSidecar(sidecar => sidecar.WithOptions(new DaprSidecarOptions { AppId = ..., DaprHttpPort = ..., DaprGrpcPort = ..., Config = daprConfigPath })).WithReference(stateStore | pubSub | secretStore).WithEnvironment(...).WaitFor(redis).WaitFor(falkordb)` shape. **Follow verbatim** for the `memories-mcp` resource. Pin distinct sidecar ports (`DaprHttpPort = 3600`, `DaprGrpcPort = 50101`) so both sidecars can coexist in the developer-laptop topology. Do **NOT** share the 3500/50001 pair used by the Server sidecar — the CommunityToolkit sidecar wiring assigns fixed ports, not ephemeral ones, and a collision silently breaks service invocation.
7. **`Dapr.Client` 1.17.6 service-invocation pattern — `Dapr.AspNetCore` wiring in `Hexalith.Memories.Server/Program.cs:37` (`builder.Services.AddDaprClient()`).** Already documented. **Reuse** via `AddDaprClient()` in the MCP Program.cs so `DaprClient` is DI-injectable. The MCP Server reaches the Memories Server via the Dapr service-invocation HTTP transport using `dapr-app-id: memories-server` headers against the local sidecar URL (`http://localhost:3500`), resolved through a named `HttpClient`. Do **NOT** hardcode `http://memories-server:5000` — the whole point of routing via the sidecar is health/retry/mTLS + token-auth interception (Story 5.4 AC3 `DAPR_API_TOKEN_MODE=enabled` applies here because service-invocation requests MUST carry `dapr-api-token` or be rejected at the sidecar boundary).
8. **`[Experimental("HXL001")]` convention — `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:267,341,402,586` + Story 9.3 reserved `HXL002`.** Diagnostic-id reservation ladder for experimental surface. **This story introduces `HXL003`** for the new `MemoriesClient.TraverseAsync` + `GetCaseAsync` methods (Task 2) — signature may change in 10.2 when token-budget shaping is wired on the server side, so opt-in callers must suppress with `#pragma warning disable HXL003`.
9. **`MemoriesRemoteException` — `src/Hexalith.Memories.Client.Rest/MemoriesRemoteException.cs` (`public sealed class MemoriesRemoteException(HttpStatusCode statusCode, ErrorResponse error)`).** Thrown by every `MemoriesClient` non-2xx path. **Reuse verbatim** — `McpErrorMapper.Map` catches it and extracts `.Error.Code` / `.Message` / `.Suggestion` for the tool-result text. Do NOT introduce a second exception type for MCP-layer errors; the existing one is the source of truth.

**What 10.1 adds:**

1. **`src/Hexalith.Memories.Mcp/`** — NEW publishable NuGet project, SDK `Microsoft.NET.Sdk.Web`. Registered in `Hexalith.Memories.slnx` under `/src/`. References `Hexalith.Memories.Contracts`, `Hexalith.Memories.Client.Rest`, `Hexalith.Memories.ServiceDefaults`, `Hexalith.Memories.Telemetry`. `IsPackable = true` (per architecture.md §Build Order Aligned to Gates row "9 | Hexalith.Memories.Mcp | LLM agent interface (Phase 1.5)"). Files:
    - **`Program.cs`** — composition root. Order: `WebApplication.CreateBuilder(args)` → `builder.AddServiceDefaults()` → `builder.Services.AddDaprClient()` → `builder.Services.AddHttpClient<MemoriesClient>(c => c.BaseAddress = new Uri("http://localhost:3500/v1.0/invoke/memories-server/method/"))` with a `MemoriesMcpDaprInvocationHandler` delegating handler that adds the `dapr-app-id: memories-server` header and (when `DAPR_API_TOKEN_MODE=enabled`) the `dapr-api-token` header from env → `builder.Services.Configure<MemoriesClientOptions>(_ => { })` (a dummy registration so the client's constructor guard holds; `BaseAddress` is set on `HttpClient`, not via options, because Dapr service-invocation URL resolution happens at the sidecar) → `builder.Services.AddMcpServer().WithHttpTransport(o => o.Stateless = true).WithTools<SearchMemoryTool>().WithTools<IngestContentTool>().WithTools<TraverseRelationsTool>().WithTools<GetCaseInfoTool>()` → `builder.Services.AddScoped<McpErrorMapper>()` → `app.MapDefaultEndpoints()` → `app.MapMcp()` → `app.Run()`. **Stateless mode is deliberate**: MCP sampling / elicitation are 10.2 concerns; 10.1 is strictly request-response, so stateless eliminates the in-memory session store and makes horizontal scaling trivial.
    - **`MemoriesMcpDaprInvocationHandler.cs`** — `internal sealed class : DelegatingHandler`. Overrides `SendAsync` to (a) add `dapr-app-id: memories-server` if absent, (b) copy `DAPR_API_TOKEN` from env into the `dapr-api-token` header when `DAPR_API_TOKEN_MODE=enabled` (Story 5.4 AC3 parity), (c) propagate the inbound MCP request's `traceparent` (`W3C` context) downstream so Story 7.5 / 8.4 trace correlation crosses the MCP-to-Server DAPR hop without a gap — use `Activity.Current?.Id` with an `ActivityContext.DidNotPropagate` guard and let `HttpClient`'s built-in OTel instrumentation add the `traceparent` header; do NOT hand-roll header injection (Story 8.4 Rev 1.0 Dev Notes §"HTTP trace context propagation" covers this — reuse the mechanism).
    - **`Tools/SearchMemoryTool.cs`** — `[McpServerToolType] internal sealed class SearchMemoryTool`. Instance-method tool (DI-resolves `MemoriesClient`, `McpErrorMapper`, `ILogger<SearchMemoryTool>`). ONE public tool method: `[McpServerTool(Name = "search_memory"), Description("Searches a tenant's memory corpus across syntactic / semantic / hybrid axes and returns scored memory-unit results. Use traverse_relations for graph traversal.")] public async Task<CallToolResult> SearchAsync([Description("The tenant identifier")] string tenantId, [Description("The natural-language or keyword string")] string query, [Description("Optional case identifier to scope the search")] string? @case = null, [Description("Search axes: syntactic | semantic | hybrid")] SearchAxis axes = SearchAxis.Hybrid, [Description("Maximum number of results to return (1-100); defaults to server default")] int maxResults = 10, [Description("Optional output token budget. 10.1 uses a conservative ~500 tokens/result estimate to narrow maxResults client-side; 10.2 will honor the budget exactly via server-side truncation. Set to 0 or omit for no budget constraint.")] int? tokenBudget = null, [Description("Whether to include explain metadata (per-axis scores, normalization details)")] bool explain = false, CancellationToken cancellationToken = default)`. **Client-side guards (10.1):** (a) `maxResults` is clamped to `[1, 100]` inside the tool method before forwarding — prevents `int.MaxValue` DoS via a crafted tool call (see "Input validation — client-side clamps" in Dev Notes); (b) `token_budget` is **DECLARED IN THE SCHEMA** (per epic AC #3) but **NOT FORWARDED TO THE SERVER** in 10.1 — the server does not yet honor it (10.2 scope). The tool applies a **client-side soft clamp** by reducing `maxResults` to `min(maxResults, token_budget / estimatedTokensPerResult)` with `estimatedTokensPerResult = 500` as the 10.1 default, so LLM clients that set `token_budget` see real behavior rather than a silently-dropped parameter. See "10.1 vs 10.2 token-budget split" in Dev Notes for rationale and the seam for the 10.2 server-side handoff.
    - **`Tools/IngestContentTool.cs`** — `[McpServerToolType] internal sealed class IngestContentTool`. `[McpServerTool(Name = "ingest_content"), Description("Ingests a content payload into a tenant's case; returns the scheduled workflow instance id.")] public async Task<CallToolResult> IngestAsync([Description("Tenant identifier")] string tenantId, [Description("Case identifier")] string caseId, [Description("The content payload (text or base64 bytes for binary)")] string content, [Description("Source type: file | url | event")] SourceType sourceType, [Description("Optional source URI (e.g., https://..., file:///...)")] string? sourceUri = null, [Description("Optional MIME content type; defaults to text/plain")] string? contentType = null, [Description("The user or system identity performing the ingestion")] string ingestedBy = "mcp", CancellationToken cancellationToken = default)`. Returns the workflow instance id in `CallToolResult` content on success.
    - **`Tools/TraverseRelationsTool.cs`** — `[McpServerToolType] internal sealed class TraverseRelationsTool`. `[McpServerTool(Name = "traverse_relations"), Description("Traverses causal / correlational relationships from a starting memory unit and returns ordered nodes + edges (plus gap markers when stubs exist).")] public async Task<CallToolResult> TraverseAsync([Description("Tenant identifier")] string tenantId, [Description("The memory unit ID to start traversal from")] string from, [Description("Maximum traversal depth (default: 3, clamped to 0-10)")] int depth = 3, [Description("Optional comma-separated list of edge types to filter (e.g., causedBy,correlatedWith)")] string? edgeType = null, [Description("Optional graph scope — restricts traversal to this case id")] string? caseId = null, CancellationToken cancellationToken = default)`. Note: the epic AC #4 `graph_scope (object, optional)` is implemented as a **single `caseId` string parameter in 10.1** — the complex-object shape is deferred because MCP tool parameters are flattened by the JSON-schema generator for simpler LLM interpretation and the existing server endpoint only accepts `caseId` + `edgeTypes`. Document the flattening in Dev Notes § "Graph scope parameter simplification".
    - **`Tools/GetCaseInfoTool.cs`** — `[McpServerToolType] internal sealed class GetCaseInfoTool`. `[McpServerTool(Name = "get_case_info"), Description("Fetches case summary (status, member count, memory-unit count, recent activity) for a given tenant + case id.")] public async Task<CallToolResult> GetCaseAsync([Description("Tenant identifier")] string tenantId, [Description("Case identifier")] string caseId, CancellationToken cancellationToken = default)`. Delegates to the new `MemoriesClient.GetCaseAsync` method (Task 2).
    - **`Tools/SearchAxis.cs`** — `internal enum SearchAxis { Syntactic, Semantic, Hybrid }`. Serializes via `MemoriesJsonContext` `CamelCaseStringEnumConverter` pattern (mirror `SourceType` enum at `Contracts/V1/SourceType.cs`). The `ModelContextProtocol.AspNetCore` schema generator emits the enum literals into the tool schema (verify in Task 7.3 contract test). Graph is deliberately omitted from `search_memory` in 10.1 because the server graph-search path requires a start memory unit; use `traverse_relations` instead.
    - **`McpErrorMapper.cs`** — `internal sealed class McpErrorMapper` with one public method: `public CallToolResult Map(MemoriesRemoteException exception, string toolName, string failedService = "memories-server")`. Returns `new CallToolResult { Content = [new TextContentBlock { Text = FormatError(exception.Error, failedService) }], StructuredContent = BuildStructured(exception.Error, failedService, toolName), IsError = true }`. Private helper `FormatError(ErrorResponse err, string service)` returns `$"[{err.Code}] (service={service}): {err.Message} {err.Suggestion}".TrimEnd()`. Private helper `BuildStructured` returns a `JsonElement` serialized from `new { code, service, tool, message, suggestion }` via `MemoriesJsonContext.Options`. Reasoning: the MCP protocol requires tool errors as `CallToolResult.IsError = true` — NOT thrown exceptions (which become generic "An error occurred invoking 'x'." messages that are useless to LLMs). Emitting BOTH prose (`TextContentBlock`) and `StructuredContent` lets LLM clients with structured error handling (Claude, GPT-4 JSON mode) route on `error.code` without regex-parsing prose, while older clients still see a human-readable line. See ModelContextProtocol docs §"Error handling" for the protocol-level rationale.
    - **`McpToolResultSerializer.cs`** — `internal static class`. `public static string Serialize<T>(T value)` uses `MemoriesJsonContext.Options`. Every tool method's return-type is `string` (JSON-serialized result) rather than a typed record because the ModelContextProtocol 1.2.0 schema generator for method returns currently emits `string` shape most reliably under AOT — typed returns trigger reflection-path warnings in AOT scenarios. Document this decision in Dev Notes § "Tool return types — string vs typed".
    - **`Hexalith.Memories.Mcp.csproj`** — `Microsoft.NET.Sdk.Web`; `IsPackable = true`; `PackageId = Hexalith.Memories.Mcp`; `Description = MCP Server surface for Hexalith.Memories (LLM agent interface — Phase 1.5).`; `Authors = ITANEO`; target framework matches solution (net10.0). Package metadata follows the `Hexalith.Memories.EventStore.csproj` template verbatim. PackageReferences: `ModelContextProtocol.AspNetCore` 1.2.0, `Dapr.AspNetCore` 1.17.6, `Dapr.Client` 1.17.6.

2. **`Directory.Packages.props`** — EDIT. Add two new `<PackageVersion>` entries:
    ```xml
    <PackageVersion Include="ModelContextProtocol" Version="1.2.0" />
    <PackageVersion Include="ModelContextProtocol.AspNetCore" Version="1.2.0" />
    ```
    Rationale for 1.2.0: latest stable (web search 2026-04-23), official Microsoft-collaboration C# SDK. Do NOT pick a 0.x preview — the 1.x line is the stable API surface. Do NOT add `ModelContextProtocol.Core` — the Mcp project needs the hosting + DI extensions shipped in the main `ModelContextProtocol` package, and `ModelContextProtocol.AspNetCore` already depends on it (the package-graph dedupe makes the `.Core` reference redundant and noisy in `.csproj` files).

3. **`src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`** — EDIT. Add two methods. Both are marked `[Experimental("HXL003")]` so the surface is opt-in pending Story 10.2 token-budget shaping:
    ```csharp
    /// <summary>Story 10.1 — fetches a graph traversal from the starting memory unit.</summary>
    /// <remarks>EXPERIMENTAL (HXL003 — Story 10.1): signature may change in 10.2 when token-budget shaping on the server side narrows the response.</remarks>
    [Experimental("HXL003")]
    public virtual async Task<TraversalResult> TraverseAsync(
        string tenantId, string startNodeId, int depth = 2,
        string? caseId = null, IReadOnlyList<EdgeType>? edgeTypes = null,
        CancellationToken ct = default)

    /// <summary>Story 10.1 — fetches case summary for <c>get_case_info</c>.</summary>
    /// <remarks>EXPERIMENTAL (HXL003 — Story 10.1).</remarks>
    [Experimental("HXL003")]
    public virtual async Task<Case> GetCaseAsync(
        string tenantId, string caseId, CancellationToken ct = default)
    ```
    Path construction: `TraverseAsync` → `GET api/tenants/{Uri.EscapeDataString(tenantId)}/traverse?startNodeId={...}&depth={...}[&caseId=...][&edgeTypes=...]` (mirror `Program.cs:2773` query shape). `GetCaseAsync` → `GET api/tenants/{Uri.EscapeDataString(tenantId)}/cases/{Uri.EscapeDataString(caseId)}` (mirror `Program.cs:1458`). Both use `MemoriesJsonContext.Options` + `ReadFromJsonAsync<T>` + `ErrorResponseDecoder.DecodeAsync` on non-2xx, exactly like `GetMemoryUnitAsync` at `MemoriesClient.cs:209`. Do NOT introduce a new JSON options instance.

4. **`src/Hexalith.Memories.AppHost/Program.cs`** — EDIT. After the existing `server` resource block (around line 117), add the `memories-mcp` project resource:
    ```csharp
    IResourceBuilder<ProjectResource> mcp = builder
        .AddProject<Projects.Hexalith_Memories_Mcp>("memories-mcp")
        .WithDaprSidecar(sidecar =>
        {
            _ = sidecar
                .WithOptions(new DaprSidecarOptions
                {
                    AppId = "memories-mcp",
                    DaprHttpPort = 3600,
                    DaprGrpcPort = 50101,
                    Config = daprConfigPath,
                });
            // Note: MCP Server intentionally does NOT receive stateStore/pubSub/secretStore
            // references. NFR11 + architecture.md §Cross-Cutting Concerns #4: "DAPR Secrets
            // scoping — Configure DAPR secret scopes so only Memories Server app-id can access
            // embedding keys. MCP Server does not have direct secret access."
        })
        .WaitFor(server);

    if (appApiToken is not null)  { mcp = mcp.WithEnvironment("APP_API_TOKEN", appApiToken); }
    if (daprApiToken is not null) { mcp = mcp.WithEnvironment("DAPR_API_TOKEN", daprApiToken); }

    _ = mcp;
    ```
    The `Projects.Hexalith_Memories_Mcp` type is generated by Aspire once the project is added to `Hexalith.Memories.AppHost.csproj`'s `<ProjectReference>` list — Task 1 adds that reference. `WaitFor(server)` guarantees the Memories Server's `/health` passes before the MCP sidecar's first inbound request.

5. **`src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj`** — EDIT. Add `<ProjectReference Include="..\Hexalith.Memories.Mcp\Hexalith.Memories.Mcp.csproj" />` under the existing `<ProjectReference>` item group so `Projects.Hexalith_Memories_Mcp` is generated for the `AddProject<T>` call in step 4.

6. **`Hexalith.Memories.slnx`** — EDIT. Add `<Project Path="src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj" />` under `/src/` and `<Project Path="tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj" />` under `/tests/`.

7. **`tests/Hexalith.Memories.Mcp.Tests/`** — NEW Tier-2 test project. SDK `Microsoft.NET.Sdk`. References `Hexalith.Memories.Mcp`, `Hexalith.Memories.Contracts`, `Hexalith.Memories.TestHelpers`. PackageReferences: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Shouldly`, `NSubstitute`, `coverlet.collector`. Files:
    - **`SearchMemoryToolTests.cs`** — tests cover: happy path syntactic axis routes to `MemoriesClient.SearchAsync`, happy path hybrid axis routes to `HybridSearchAsync`, missing `tenantId` returns `CallToolResult.IsError == true` with `INVALID_INPUT`, server `TENANT_NOT_FOUND` maps to a protocol error result, `explain=true` propagation, `axes` accepts `syntactic`/`semantic`/`hybrid` only, `maxResults` clamps to `[1, 100]`, and `tokenBudget` narrows forwarded `maxResults`.
    - **`IngestContentToolTests.cs`** — 4 tests: happy path returns workflow instance id, malformed payload → error mapped, tenant-suspended → mapped, rate-limited 429 → mapped with `RATE_LIMITED` code.
    - **`TraverseRelationsToolTests.cs`** — 4 tests: happy path returns node+edge JSON, invalid `edgeType` value → error mapped with `INVALID_EDGE_TYPE` **without calling the server** (client-side reject-gate), `depth` client-side clamped (`depth = 100` → forwarded value is 10; `depth = -1` → forwarded value is 0), missing `from` → error mapped.
    - **`GetCaseInfoToolTests.cs`** — 2 tests: happy path returns case summary, case-not-found → mapped.
    - **`McpErrorMapperTests.cs`** — 8 tests: (a) basic `ErrorResponse` → text format matches `[CODE] (service=X): message suggestion`, (b) empty Suggestion produces trailing-space-free text, (c) null/whitespace `failedService` defaults to `memories-server`, (d) `IsError = true` always, (e) both `TextContentBlock` AND `StructuredContent` emitted on error, (f) `MapGeneric_DoesNotLeakStackTrace` — security gate, (g) `[Theory]` `MapGeneric_DoesNotEchoInputValues` with 5 payload classes (path traversal, SQL fragment, script tag, null bytes, 10KB string) — security gate, (h) `StructuredContent_ToolField_IsLiteralToolName` — closes the tool-name echo channel.
    - **`McpUnauthenticatedStartupGuardTests.cs`** — 4 tests (AC 11 guard): `Validate_Throws_WhenNotDevelopmentAndOptInUnset`, `Validate_Allows_WhenDevelopment`, `Validate_Allows_WhenOptInTrue`, `LogStartupWarning_EmitsWarningLevelMessage`.
    - **`McpToolSchemaTests.cs`** — Tier-1 contract tests (Task 7). ONE test per tool asserts: (a) tool is discoverable by name via `ListToolsAsync` on an in-process `McpClient`, (b) every parameter has a non-empty `description` that is longer than the parameter name, contains a space, and is not a case-insensitive match for the parameter name (FR58 non-trivial-prose gate), (c) required parameters are marked `required` in the schema, (d) enum parameters emit `enum` constraint with the 4 axis values, (e) default values are present for optional parameters with defaults.
    Use `NSubstitute.Substitute.For<MemoriesClient>(...)` + `Shouldly` assertions, exactly like `tests/Hexalith.Memories.Cli.Tests/` does (mirror that project's `Directory.Build.props` inheritance).

8. **`tests/Hexalith.Memories.IntegrationTests/Mcp/`** — NEW folder. ONE Tier-3 test file:
    - **`McpServerIntegrationTests.cs`** — `[Collection("Aspire")]`. Uses the existing `DistributedApplicationTestingBuilder` fixture (`tests/Hexalith.Memories.IntegrationTests/Fixtures/` — follow the pattern of `tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs`). Two acceptance tests:
      - **`ListTools_EndToEnd_ReturnsFourToolsWithTypedSchemas`** — resolves the `memories-mcp` resource endpoint via `app.GetEndpoint("memories-mcp", "http")`, creates an `McpClient` via `HttpClientTransport`, calls `ListToolsAsync()`, asserts exactly 4 tool names (`search_memory`, `ingest_content`, `traverse_relations`, `get_case_info`), asserts every tool's schema has a non-empty `description` (FR58) and typed parameters (FR58).
      - **`CallSearchMemory_EndToEnd_ExecutesAcrossDaprHop`** — creates a tenant + case via the Server REST API (Test setup), ingests 3 memory units, then calls `McpClient.CallToolAsync("search_memory", new Dictionary<string, object?> { ["tenantId"] = ..., ["query"] = ..., ["axes"] = "hybrid" })`, asserts `IsError == false` and result JSON deserializes to `HybridSearchResult` with ≥1 scored result. The DAPR hop is implicit — the Aspire fixture provides both sidecars.
    Do NOT attempt to assert OpenTelemetry trace-hop propagation in 10.1 — that's a Story 7.5 / 8.4 concern and the `AspireEndToEndTraceTests` suite already covers the CLI → ingress → Server → backends chain. Adding an MCP-specific trace-hop test is tempting but out-of-scope; a follow-up story can extend `AspireEndToEndTraceTests` once 10.2 auth is wired.

9. **`deploy/dapr/config.yaml`** — verify no edits needed. The Story 5.4 AC3 DAPR configuration already covers both sidecars (the `DAPR_API_TOKEN_MODE=enabled` toggle in `AppHost/Program.cs:229` applies to both). If the config.yaml has an explicit `spec.tracing.zipkin.endpointAddress`, both sidecars emit traces to the same endpoint — no per-sidecar override needed. **Document "no change required" in Dev Notes** so implementers do not waste time poking at this file.

10. **Docs** — NEW developer guide `docs/dev/mcp-server.md` covering: (a) what the MCP Server is and is not (LLM-agent tool surface, NOT a general REST gateway), (b) the 4 tools + their parameter schemas + one example call each, (c) the DAPR service-invocation hop from MCP to Server and how `dapr-app-id` routes the call, (d) local dev workflow (`dotnet run --project src/Hexalith.Memories.AppHost` boots both services; `mcp-client` — any compliant MCP client like Claude Desktop or a custom `McpClient` — connects to `http://localhost:{mcp-port}/mcp`), (e) error-response shape and how LLM agents should interpret `[CODE] (service=X): message suggestion`, (f) a forward-looking note that token-budget shaping + ingress auth ship in 10.2. The worked example stays within 10.1 scope — no token-budget claims.

11. **`deferred-work.md`** update: add entries marking **token-budget shaping** (epic AC — `token_budget` parameter declared in schema but not forwarded), **degraded-state annotations** (`degraded: true` + excluded-axes list), **ingress authentication** (NFR11), and **MCP-specific trace-hop assertion** as explicitly deferred to Story 10.2 / a follow-up story so implementation does not drift back into them.

**What does NOT ship:**

- **Token-budget response truncation.** The `token_budget` parameter is declared in the `search_memory` tool schema so the schema shape is stable from day one, but it is NOT forwarded to the server side in 10.1 — the server doesn't honor it yet. Story 10.2 wires the server-side `token_budget` → `maxResults` translation + `omitted_count` in the response.
- **`traverse_relations` token-budget truncation.** Same as above — 10.2.
- **`degraded: true` response shape when a backend is unavailable.** Story 10.2 Epic AC #6 — requires server-side plumbing of the per-axis availability status into the search response; 10.1 returns whatever shape the Server's existing `/api/search` returns, which does NOT currently emit a `degraded` flag.
- **Ingress authentication (NFR11).** No JWT or `AddMcp()`-configured bearer auth in 10.1 — the MCP endpoint is reachable from the Aspire developer topology and from Aspire Testing without authentication. Story 10.2 AC "Given an external LLM agent connecting to the MCP Server" adds `AddAuthentication("Bearer")` + `AddMcp()` + `AddAuthorizationFilters()` per the ModelContextProtocol docs §"Configure MCP Server with Authorization".
- **`ClaimsPrincipal` parameter injection into tools.** Because auth is 10.2 scope, the tools do NOT yet take a `ClaimsPrincipal?` parameter. Adding it prematurely means the schema churns when 10.2 wires auth (the SDK auto-excludes `ClaimsPrincipal` from the emitted schema, but the method-signature change still counts as API surface churn).
- **MCP sampling / elicitation.** The MCP protocol supports server-to-client requests (e.g., asking the LLM to generate text, prompting the user for input). Story 10.1 runs in **Stateless mode** (`WithHttpTransport(o => o.Stateless = true)`) which disables these — stateless is documented as recommended for thin request-response servers. Non-stateless support is a future story only if a concrete tool needs it.
- **Custom `CallTool` request filters.** The `WithRequestFilters(...)` surface is NOT used in 10.1. Error handling happens inside each tool method via `try { ... } catch (MemoriesRemoteException ex) { return _mapper.Map(ex, toolName); }` — explicit + unit-testable. A centralized filter is a 10.2 refactor candidate when cross-cutting concerns (auth + token budget + telemetry) pile up.
- **MCP resources or prompts primitives.** The protocol supports resources (`resources/list`, `resources/read`) and prompts (`prompts/list`, `prompts/get`) in addition to tools. Story 10.1 registers ONLY tools. The 4 acceptance-criteria items in the epic are all tools, not resources or prompts — adding resources/prompts would be feature creep.
- **OpenAPI-spec import / dynamic tool generation.** Each tool is a hand-written `[McpServerToolType]` method. Dynamic tool generation from the Server's `openapi.json` is tempting but fragile — the server's minimal-API endpoints don't emit a first-class OpenAPI document today, and generating one just to re-read it is indirection without benefit.
- **Rate limiting on the MCP endpoint.** Story 6.2 already rate-limits per-tenant embedding calls at the Server level. The MCP Server inherits that gate implicitly via the DAPR service-invocation hop. MCP-endpoint-level rate limiting (per-connection, per-API-key) is a 10.2 concern bundled with auth.
- **A published `memories mcp` CLI subcommand.** No `memories-cli` changes. The CLI remains the operator surface (7.x); MCP is the LLM surface. Bridging them is architecture creep — an operator wants `memories search`; an LLM agent wants `search_memory`. Different personas, different tools.

**Primary risks:**

- **Risk #1 (high) — DAPR service-invocation URL shape drift under Aspire Testing.** The `http://localhost:3500/v1.0/invoke/memories-server/method/` base address works in AppHost-orchestrated local dev because the MCP sidecar is at `DaprHttpPort = 3600` but responds to invocations targeted at other app-ids via its service-invocation path. Under `DistributedApplicationTestingBuilder`, the sidecars may bind to randomized ports. **Mitigation:** inject the sidecar URL via `DaprClient` (`DaprClient.InvokeMethodAsync<TRequest, TResponse>(HttpMethod.Get, "memories-server", "api/tenants")`) instead of hard-coding `http://localhost:3500/...` in the `HttpClient.BaseAddress`. Use an explicit `DaprClientBuilder().UseHttpEndpoint(Environment.GetEnvironmentVariable("DAPR_HTTP_ENDPOINT") ?? "http://localhost:3500")` chain — the `DAPR_HTTP_ENDPOINT` env var is set automatically by CommunityToolkit.Aspire.Hosting.Dapr when the sidecar is provisioned. Guard test: `McpServerIntegrationTests.CallSearchMemory_EndToEnd_ExecutesAcrossDaprHop` runs under Aspire Testing and will fail hard if the port resolution is wrong.
- **Risk #2 (medium) — ModelContextProtocol 1.2.0 AOT compatibility.** The SDK uses reflection for schema generation from method signatures. If the MCP Server is built AOT (`<PublishAot>true</PublishAot>`), reflection-based schema gen may warn or fail. **Mitigation:** 10.1 does NOT set `<PublishAot>true</PublishAot>` in `Hexalith.Memories.Mcp.csproj` — default JIT. The other published packages (`Contracts`, `Client.Rest`, `EventStore`, `Cli`) also do not mandate AOT. AOT for MCP is a future concern tracked under `Story-10.x-McpAotCompatibility` in `deferred-work.md` — add that entry.
- **Risk #3 (medium) — MCP tool method signature vs. JSON-schema descriptor drift.** The SDK emits the schema from C# `Description` attributes at schema-generation time (either build time via source generator or first-call time). If a developer renames a parameter WITHOUT updating the description, downstream LLM agents see a stale description. **Mitigation:** Task 7.3 is a contract test that asserts every tool has FR58-compliant descriptions on every parameter. The test lives in `Hexalith.Memories.Mcp.Tests/McpToolSchemaTests.cs` and fails the build on drift. No silent degradation.
- **Risk #4 (medium) — Error-mapping loss of fidelity for non-`MemoriesRemoteException` failures.** `McpErrorMapper.Map` handles `MemoriesRemoteException`. But `HttpRequestException` (network-level), `TaskCanceledException` (timeout), and raw exceptions from the DAPR sidecar (e.g., "app-id not found") bypass the mapper and surface as generic `An error occurred invoking 'X'.` messages per the ModelContextProtocol docs §"Handle Tool Errors with ArgumentException". **Mitigation:** Task 4.8 extends `McpErrorMapper` with a `MapGeneric(Exception ex, string toolName)` overload that wraps any exception type, and every tool method's catch block uses `catch (Exception ex) { return _mapper.MapGeneric(ex, toolName); }` as the outer wrapper AFTER the specific `catch (MemoriesRemoteException)` block. Guard test: `McpErrorMapperTests.MapGeneric_HandlesHttpRequestException_ReturnsIsErrorWithNetworkCode`.
- **Risk #5 (low) — Duplicate `traceparent` header on the DAPR invocation hop.** ASP.NET Core's default `HttpClient` OTel instrumentation auto-injects `traceparent`; the DAPR sidecar also injects it. If both run, the downstream span may see a stale parent. **Mitigation:** rely on the HttpClient injection (which is the ambient `Activity.Current`) and let the sidecar observe the header as-is without re-injecting. Do NOT disable `HttpClient` OTel instrumentation in `ServiceDefaults` — that would break Story 7.5 / 8.4 trace continuity. The breadcrumb test in `AspireEndToEndTraceTests` (Story 8.4) will surface any duplication at code-review time if it occurs.
- **Risk #6 (low) — `MemoriesClient` is registered with a `HttpClient.BaseAddress` but Dapr service-invocation URLs differ by operation.** If Task 3 uses `HttpClient.BaseAddress = "http://localhost:3500/v1.0/invoke/memories-server/method/"` and Dapr rebinds to a different port under test, the client breaks. **Mitigation:** use `IHttpClientFactory.CreateClient("memories-server")` + `DaprClient.CreateInvokeHttpClient("memories-server")` — the 1.17.6 `Dapr.Client` SDK ships `CreateInvokeHttpClient(appId, daprEndpoint)` that handles port resolution + `dapr-app-id` header injection in one call. Do NOT hand-roll the base-address string.
- **Risk #7 (low) — `McpServerToolType` registration order.** `.WithTools<T>()` calls register tools in order; if two tools share a name (e.g., from a typo), registration throws at startup. **Mitigation:** Tool names are the LITERAL `[McpServerTool(Name = "snake_case")]` attribute values listed above — all 4 are distinct. Guard test: `McpToolSchemaTests.AllToolsHaveDistinctNames` iterates the registered tools and asserts 4 distinct names.
- **Risk #8 (low) — `[Experimental("HXL003")]` propagation to Mcp.** The MCP tool methods call `MemoriesClient.TraverseAsync` + `MemoriesClient.GetCaseAsync`, both `[Experimental("HXL003")]`. The callers in `Hexalith.Memories.Mcp` must suppress the diagnostic. **Mitigation:** wrap the two call sites with `#pragma warning disable HXL003` / `#pragma warning restore HXL003` at the method level in `TraverseRelationsTool.TraverseAsync` and `GetCaseInfoTool.GetCaseAsync`. Do NOT suppress globally — keep the opt-in explicit.

## Story

As a developer,
I want an MCP server that exposes memory capabilities as typed tools for LLM agents,
So that AI assistants can search, ingest, traverse, and query case information programmatically.

## Acceptance Criteria

**From the Epic:**

1. **Given** the MCP Server is deployed as a DAPR service (app-id: `memories-mcp`), **When** it starts and registers with the Aspire AppHost, **Then** it has its own DAPR sidecar and communicates with Memories Server via DAPR service invocation. **And** the Aspire Dashboard shows the MCP Server as a healthy service.
2. **Given** an LLM agent connects to the MCP Server, **When** it queries available tools, **Then** the following tools are registered: `search_memory`, `ingest_content`, `traverse_relations`, `get_case_info` (FR54). **And** each tool has typed parameter schemas with descriptions suitable for LLM consumption (FR58).
3. **Given** the `search_memory` tool schema, **When** inspected by an LLM agent, **Then** it includes typed parameters: `query` (string, required), `case` (string, optional), `axes` (enum: `syntactic`/`semantic`/`hybrid`, default: `hybrid`), `token_budget` (integer, optional), `explain` (boolean, optional). **And** each parameter has a description explaining its purpose. *(Note: `token_budget` is declared in the schema for API stability but is NOT forwarded to the server in 10.1 — Story 10.2 wires server-side truncation. This is a deliberate contract-stability choice. Graph traversal is exposed through `traverse_relations` because it requires a start memory unit.)*
4. **Given** the `traverse_relations` tool schema, **When** inspected, **Then** it includes: `from` (string, required — memory unit ID), `depth` (integer, default: 3), `edge_type` (string, optional — comma-separated edge type names), `graph_scope` (string, optional). *Note: this is an **acknowledged, deliberate deviation** from the original epic wording — the epic said `graph_scope (object, optional)` but 10.1 flattens it to a single `caseId` string parameter for LLM ergonomics and server-contract alignment. See Dev Notes § "Graph scope parameter simplification" for rationale. A future requirements validator should treat this as a planned simplification, not a regression.*
5. **Given** any MCP tool request and response, **When** validated against the MCP protocol specification, **Then** the following concrete shape holds (NFR20):
    - `ListToolsAsync` returns exactly 4 tools whose names match the `search_memory` / `ingest_content` / `traverse_relations` / `get_case_info` set.
    - Every tool's JSON schema declares each parameter's `type`, `description`, and (where applicable) `default`, `enum`, or `required` constraints.
    - `CallToolAsync` returns a `CallToolResult` whose `Content[0].Type == "text"` on success; on failure `IsError == true` AND both a `TextContentBlock` and `StructuredContent` are present.
    - Requests sent over the `/mcp` Streamable HTTP endpoint succeed with the `ModelContextProtocol` SDK 1.2.0 client (canonical reference implementation — conformance is measured against the SDK's own client, not a hand-rolled parser). Scope limits (no resources/prompts/sampling primitives) are documented in "What does NOT ship".
6. **Given** a tool call that results in an error, **When** the MCP Server returns the error, **Then** it maps the Hexalith error code to MCP format with the failed service identifier. **And** the error is structured for LLM interpretation (not raw stack traces).

**Added in 10.1 for disaster prevention (operator-facing / guard-test-facing):**

7. **Given** the MCP Server is running under the Aspire AppHost, **When** the DAPR sidecar for `memories-server` is down, **Then** the `memories-mcp` `/ready` health check reports `Unhealthy` with a diagnostic detail pointing to the failing upstream (via the inherited `DaprSidecarHealthCheck` from `ServiceDefaults` plus a new upstream-probe check).
8. **Given** `DAPR_API_TOKEN_MODE=enabled` is set, **When** the MCP Server invokes the Memories Server, **Then** the request carries the `dapr-api-token` header sourced from `DAPR_API_TOKEN` env and the sidecar honors it (Story 5.4 AC3 parity).
9. **Given** the full 4-tool schema is served, **When** inspected, **Then** every parameter has a non-empty `description` (FR58 enforcement via `McpToolSchemaTests.AllParametersHaveDescriptions`).
10. **Given** the 4 tool names are registered, **When** inspected via `ListToolsAsync`, **Then** no two tools share a name and all 4 expected names (`search_memory`, `ingest_content`, `traverse_relations`, `get_case_info`) are present.
11. **Given** the MCP Server's unauthenticated 10.1 state, **When** the app starts with `ASPNETCORE_ENVIRONMENT` set to any value OTHER than `Development` AND the explicit opt-in env var `MEMORIES_MCP_ALLOW_UNAUTHENTICATED` is unset or not `true`, **Then** startup fails fast with a clear `InvalidOperationException` whose message names the required opt-in. **And** a `LogLevel.Warning` line is written on successful startup naming the unauthenticated surface (visible to operators doing `kubectl logs`). This is a code-layer safety net on top of the docs warning (defense in depth — a misconfigured staging deployment cannot silently expose `/mcp`).

## Tasks / Subtasks

- [x] **Task 1 — Scaffold `Hexalith.Memories.Mcp` project** (AC: 1, 2)
  - [x] 1.1 Create `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj` (SDK `Microsoft.NET.Sdk.Web`, `IsPackable = true`, `PackageId = Hexalith.Memories.Mcp`, NuGet metadata mirroring `Hexalith.Memories.EventStore.csproj`).
  - [x] 1.2 Add `<ProjectReference>` entries for `Hexalith.Memories.Contracts`, `Hexalith.Memories.Client.Rest`, `Hexalith.Memories.ServiceDefaults`, `Hexalith.Memories.Telemetry`.
  - [x] 1.3 Add `<PackageReference>` entries for `ModelContextProtocol.AspNetCore`, `Dapr.AspNetCore`, `Dapr.Client`.
  - [x] 1.4 Edit `Directory.Packages.props` to add `ModelContextProtocol` 1.2.0 and `ModelContextProtocol.AspNetCore` 1.2.0.
  - [x] 1.5 Register the project + tests project in `Hexalith.Memories.slnx`.
  - [x] 1.6 Verify `dotnet restore` + `dotnet build src/Hexalith.Memories.Mcp` succeeds with zero warnings (honors `TreatWarningsAsErrors`). **Explicitly check for NU1605 (package downgrade) and NU1608 (version conflict) warnings** arising from the `Dapr.Client` 1.17.6 + `ModelContextProtocol.AspNetCore` 1.2.0 transitive-dependency overlap (`Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions`). If any surface, pin the shared dependency at the higher compatible version in `Directory.Packages.props` rather than suppressing the warning.

- [x] **Task 2 — Extend `MemoriesClient` with `TraverseAsync` + `GetCaseAsync`** (AC: 4)
  - [x] 2.1 Add `[Experimental("HXL003")] public virtual async Task<TraversalResult> TraverseAsync(...)` mirroring the `GET /api/tenants/{tenantId}/traverse` query shape at `Server/Program.cs:2773`.
  - [x] 2.2 Add `[Experimental("HXL003")] public virtual async Task<Case> GetCaseAsync(...)` mirroring `GET /api/tenants/{tenantId}/cases/{caseId}` at `Server/Program.cs:1458`.
  - [x] 2.3 Verify `TraversalResult` and `Case` are already registered in `MemoriesJsonContext` (they should be — both are Contracts.V1 types used by existing endpoints).
  - [x] 2.4 Add 4 unit tests to `tests/Hexalith.Memories.Cli.Tests/ClientRest/` (this is the established location for `MemoriesClient` tests — precedent: `MemoriesClientSearchTests.cs`, `MemoriesClientConsistencyTests.cs`, `MemoriesClientExportTests.cs`; the name reflects historical coupling to the CLI, not domain boundary) mirroring `SearchMemoryTool`-equivalent cases for the two new methods (happy path + 404 + 500 + network error).
  - [x] 2.5 Suppress `HXL003` at individual call sites in test projects using `#pragma warning disable HXL003` / `#pragma warning restore HXL003` (mirror the `HXL001` precedent at `tests/Hexalith.Memories.Cli.Tests/Cli/MemoriesClientWorkflowResponseTests.cs:35-37`). Do NOT add project-level `<NoWarn>HXL003</NoWarn>` — the per-call-site opt-in is intentional to keep experimental usage visible in review.

- [x] **Task 3 — Implement MCP Server composition root** (AC: 1)
  - [x] 3.1 `Program.cs` in `Hexalith.Memories.Mcp/` — `AddServiceDefaults()` + `AddDaprClient()` + `AddMcpServer().WithHttpTransport(o => o.Stateless = true).WithTools<...>()` + tool registrations + `AddScoped<McpErrorMapper>()` + `MapDefaultEndpoints()` + `MapMcp()`.
  - [x] 3.2 Register `MemoriesClient` as a typed `HttpClient` using `DaprClient.CreateInvokeHttpClient("memories-server")` — NOT a hand-rolled base address. This handles port resolution + `dapr-app-id` header injection automatically.
  - [x] 3.3 Add `MemoriesMcpDaprInvocationHandler` delegating handler that injects `dapr-api-token` when `DAPR_API_TOKEN_MODE=enabled`.
  - [x] 3.4 Verify the app builds as a Kestrel-hosted ASP.NET Core app (no custom `IHostedService` machinery required — `MapMcp()` wires the MCP endpoint as middleware).
  - [x] 3.5 **DI-shape spike (30 min, pre-implementation).** Before committing to the singleton-factory `MemoriesClient` registration in 3.2, spike the alternative `AddHttpClient<MemoriesClient>(cfg)` typed-client shape against `DaprClient.CreateInvokeHttpClient`. If the typed-client shape works (pipeline intact, `dapr-app-id` header preserved), prefer it — it keeps `IHttpClientFactory` logging + metrics that `ServiceDefaults` expects. Document the spike outcome in Dev Notes (overwrite "DI shape for `MemoriesClient`" with the validated approach). Do NOT proceed to 3.2 until the spike result is captured.
  - [x] 3.6 **Startup environment gate (AC 11).** Add a `ValidateUnauthenticatedEnvironment(builder.Environment, builder.Configuration)` call immediately after `WebApplication.CreateBuilder(args)` that: (a) throws `InvalidOperationException("MCP 10.1 is unauthenticated. Set MEMORIES_MCP_ALLOW_UNAUTHENTICATED=true to run outside Development, or wait for Story 10.2.")` when `!builder.Environment.IsDevelopment()` AND `builder.Configuration["MEMORIES_MCP_ALLOW_UNAUTHENTICATED"] != "true"`; (b) after successful validation and `app.MapMcp()`, writes a single `LogWarning("MCP endpoint /mcp is UNAUTHENTICATED (10.1). Do not expose outside a trusted network. Story 10.2 adds ingress auth.")` at startup so operators see it in `kubectl logs` / Aspire Dashboard. Implement as an internal static class `McpUnauthenticatedStartupGuard` with two methods: `Validate` and `LogStartupWarning`. Tests: see Task 8.7.
  - [x] 3.7 **Evaluate `MemoriesMcpDaprInvocationHandler` necessity (10-min check).** If `DaprClient.CreateInvokeHttpClient("memories-server")` handles `dapr-app-id` injection automatically and the only remaining need is `dapr-api-token`, consider setting `HttpClient.DefaultRequestHeaders.Add("dapr-api-token", ...)` in the DI factory and **deleting the `MemoriesMcpDaprInvocationHandler` class entirely**. A `DelegatingHandler` is worth the complexity only when per-request logic is needed (none in 10.1). Document the decision — keep or delete — in Dev Notes § "Dapr invocation handler — keep or delete".

- [x] **Task 4 — Implement 4 tool classes + `McpErrorMapper`** (AC: 2, 3, 4, 5, 6, 9)
  - [x] 4.1 `Tools/SearchMemoryTool.cs` — `[McpServerToolType]` + one tool method per the spec above, axes-routing to `SearchAsync` / `HybridSearchAsync`. Declare the soft-clamp heuristic as a named constant `internal const int EstimatedTokensPerResult = 500;` on the class with an XML `<remarks>` comment noting it is a 10.1 heuristic replaced in 10.2 by server-measured truncation. Do NOT inline the literal `500` anywhere else — tests reference the const so impl+test stay in lockstep.
  - [x] 4.2 `Tools/IngestContentTool.cs` — delegates to `MemoriesClient.IngestAsync`.
  - [x] 4.3 `Tools/TraverseRelationsTool.cs` — delegates to `MemoriesClient.TraverseAsync` (Task 2); parses `edgeType` comma-separated string into `EdgeType[]`; `#pragma warning disable HXL003` wrapped.
  - [x] 4.4 `Tools/GetCaseInfoTool.cs` — delegates to `MemoriesClient.GetCaseAsync` (Task 2); `#pragma warning disable HXL003` wrapped.
  - [x] 4.5 `Tools/SearchAxis.cs` — `internal enum` + camelCase JSON converter registration.
  - [x] 4.6 `McpErrorMapper.cs` — `Map(MemoriesRemoteException, string, string = "memories-server")` + `MapGeneric(Exception, string)` + `FormatError(ErrorResponse, string)` private helper.
  - [x] 4.7 `McpToolResultSerializer.cs` — wraps `JsonSerializer.Serialize(value, MemoriesJsonContext.Options)`.
  - [x] 4.8 Every tool method's body wraps its single delegation in `try { ... } catch (MemoriesRemoteException ex) { ... _mapper.Map(ex, toolName) ... } catch (Exception ex) { ... _mapper.MapGeneric(ex, toolName) ... }` — the outer wrapper is the Risk #4 mitigation.

- [x] **Task 5 — AppHost wiring** (AC: 1, 7, 8)
  - [x] 5.1 Edit `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` to add `<ProjectReference Include="..\Hexalith.Memories.Mcp\Hexalith.Memories.Mcp.csproj" />`.
  - [x] 5.2 Edit `src/Hexalith.Memories.AppHost/Program.cs` to add the `memories-mcp` project resource + sidecar with `AppId = "memories-mcp"`, `DaprHttpPort = 3600`, `DaprGrpcPort = 50101`, `Config = daprConfigPath`. Do NOT add `.WithReference(stateStore | pubSub | secretStore)` — MCP isolation per NFR11.
  - [x] 5.3 Propagate `APP_API_TOKEN` + `DAPR_API_TOKEN` env vars to `memories-mcp` when `DAPR_API_TOKEN_MODE=enabled` (Story 5.4 AC3 parity).
  - [x] 5.4 Add `.WaitFor(server)` to block MCP startup on Memories Server health.

- [x] **Task 6 — Health checks + readiness** (AC: 1, 7)
  - [x] 6.1 Add `DaprSidecarHealthCheck` to `Hexalith.Memories.Mcp/Program.cs` the same way `Server/Program.cs:42` does (tags: `live`, `ready`).
  - [x] 6.2 **Mandatory** upstream-probe check (required by AC 7): call `MemoriesClient.ProbeHealthAsync` (already exists at `MemoriesClient.cs:508`, returns `Task<bool>` — do NOT add; the `bool` shape is sufficient for 10.1's rolling-window logic) from a new `MemoriesServerUpstreamHealthCheck` class; tag `ready`; treat `false` as a failure tick in the rolling-window accumulator; on a single `false` tick return `Degraded` (NOT `Unhealthy`) so transient Server hiccups don't flap the MCP Aspire Dashboard row; on sustained failure (>3 consecutive `false` ticks via a rolling-window accumulator) return `Unhealthy` with a diagnostic `data["upstream"] = "memories-server"`.
  - [x] 6.3 Verify `/health`, `/alive`, `/ready` respond 200/503 correctly under Aspire local dev.

- [x] **Task 7 — Tier-1 contract tests** (AC: 2, 3, 4, 9, 10)
  - [x] 7.1 `Hexalith.Memories.Mcp.Tests/McpToolSchemaTests.cs::ListToolsAsync_ReturnsExactlyFourTools` — assert 4 names exist (AC 10).
  - [x] 7.2 `::AllToolsHaveDistinctNames` — no dupes (AC 10 guard).
  - [x] 7.3 `::AllParametersHaveDescriptions` — iterate schemas, assert every parameter has a non-empty `description` AND the description is more than a trivial echo of the parameter name (AC 9 / FR58). Concrete assertions: (a) `description.Length > parameterName.Length`, (b) `description.Contains(' ')` (proves it is prose, not a single identifier), (c) `!description.Equals(parameterName, StringComparison.OrdinalIgnoreCase)`. Rationale: MCP SDK 1.2.0 falls back to the parameter name when no `[Description]` is declared, producing technically-non-empty-but-useless descriptions that a naive length check misses.
  - [x] 7.4 `::SearchMemoryTool_AxesParameter_EmitsEnumWithThreeValues` — assert `axes` schema lists `syntactic`/`semantic`/`hybrid` and does not advertise graph search (AC 3 review close-out).
  - [x] 7.5 `::SearchMemoryTool_TokenBudget_PresentAsOptionalInteger` — schema shape stability guard (AC 3 — declared even if not forwarded).
  - [x] 7.6 `::TraverseRelationsTool_GraphScope_IsCaseIdString` — document the 10.1 simplification (AC 4).
  - [x] 7.7 `::SearchMemoryTool_QueryParameter_IsRequired` — required-field schema check.

- [x] **Task 8 — Tier-2 tool unit tests** (AC: 5, 6, 8)
  - [x] 8.1 `SearchMemoryToolTests` — 6 tests per the spec.
  - [x] 8.2 `IngestContentToolTests` — 4 tests.
  - [x] 8.3 `TraverseRelationsToolTests` — 4 tests.
  - [x] 8.4 `GetCaseInfoToolTests` — 2 tests.
  - [x] 8.5 `McpErrorMapperTests` — 7+ tests: (a)-(e) per the spec above, PLUS (f) `MapGeneric_DoesNotLeakStackTrace` — asserts the `Text` content block contains a sanitized prefix (`[NETWORK_ERROR] (service=memories-server): ...`) and does NOT contain `at System.` / `---> System.` / `StackTrace` markers (security gate — no internal path disclosure to LLM clients or their telemetry sinks), PLUS (g) `MapGeneric_DoesNotEchoInputValues` as a `[Theory]` covering **five input classes**: path traversal (`../../etc/passwd`), SQL fragment (`'; DROP TABLE cases;--`), script tag (`<script>alert(1)</script>`), null bytes (`"abc\0def"`), and an oversized payload (>10KB random string). For each, construct an exception whose `.Message` contains the payload and assert the returned `Text` does NOT contain the payload substring. Rationale: `ex.Message` can echo user-supplied input and smuggle it to LLM telemetry sinks; the mapper must either strip or bucket the message into a known safe phrase (e.g., `"input validation failed"`) when it detects caller data. PLUS (h) `StructuredContent_ToolField_IsLiteralToolName` — assert the `StructuredContent` object's `tool` property equals the literal tool name passed to `Map(..., toolName, ...)` and is never derived from user input. Closes the Security-Reviewer-flagged echo channel.
  - [x] 8.6 `MemoriesMcpDaprInvocationHandlerTests` — 3 tests (Risk #8 + AC 8 guard): (a) `dapr-api-token` header added when `DAPR_API_TOKEN_MODE=enabled` and `DAPR_API_TOKEN` is set, (b) `dapr-api-token` header absent when `DAPR_API_TOKEN_MODE` is unset or not `enabled`, (c) `dapr-app-id: memories-server` header always present regardless of token mode. Use `TestHttpMessageHandler` + `Environment.SetEnvironmentVariable` (with cleanup) to drive scenarios. **Skip this entire test file if Task 3.7 concludes the handler class is deleted** — in that case, the equivalent guarantees are covered by the `HttpClient.DefaultRequestHeaders` configuration in the DI factory, and a single integration assertion ("the outbound request carries `dapr-api-token`") replaces the three unit tests.
  - [x] 8.7 `McpUnauthenticatedStartupGuardTests` — 4 tests (AC 11 guard): (a) `Validate_Throws_WhenNotDevelopmentAndOptInUnset` — `ASPNETCORE_ENVIRONMENT=Production` + no opt-in → `InvalidOperationException` whose message names `MEMORIES_MCP_ALLOW_UNAUTHENTICATED`; (b) `Validate_Allows_WhenDevelopment` — `ASPNETCORE_ENVIRONMENT=Development` → no throw regardless of opt-in; (c) `Validate_Allows_WhenOptInTrue` — `ASPNETCORE_ENVIRONMENT=Production` + `MEMORIES_MCP_ALLOW_UNAUTHENTICATED=true` → no throw; (d) `LogStartupWarning_EmitsWarningLevelMessage` — asserts a single `LogLevel.Warning` line whose text contains "UNAUTHENTICATED". Use `TestLoggerProvider` to capture log output.

- [x] **Task 9 — Tier-3 Aspire integration tests** (AC: 1, 2, 5)
  - [x] 9.1 `IntegrationTests/Mcp/McpServerIntegrationTests.cs::ListTools_EndToEnd_ReturnsFourToolsWithTypedSchemas`.
  - [x] 9.2 `::CallSearchMemory_EndToEnd_ExecutesAcrossDaprHop` — in addition to asserting `IsError == false` and `HybridSearchResult` deserialization, assert the outbound trace contains a span whose `http.url` (or `peer.service`) resolves to the DAPR sidecar invocation path (`/v1.0/invoke/memories-server/method/*`). This proves the request traversed the sidecar rather than routing directly to the Server container — a misconfigured `HttpClient.BaseAddress` could otherwise make the test pass falsely.
  - [x] 9.3 Verify the test fixture (`tests/Hexalith.Memories.IntegrationTests/Fixtures/`) supports adding the `memories-mcp` resource — it may already work via `DistributedApplicationTestingBuilder` automatic discovery from AppHost; if not, document the fixture change in Dev Notes.

- [x] **Task 10 — Docs + sprint-status + retro** (AC: all)
  - [x] 10.1 NEW `docs/dev/mcp-server.md` per the spec above. **Must include a bold security warning at the top of the document** (before the "what the MCP Server is and is not" section): "⚠️ **UNAUTHENTICATED in 10.1** — the `/mcp` endpoint has NO ingress authentication in Story 10.1. Do NOT expose the MCP port outside a trusted network. Route all external traffic through the Server REST ingress until Story 10.2 lands (`AddAuthentication('Bearer')` + `AddMcp()` + `AddAuthorizationFilters()`)." This warning protects operators from inadvertently exposing the developer-topology endpoint in staging / prod environments.
  - [x] 10.2 Update `_bmad-output/implementation-artifacts/deferred-work.md` with 5 new entries — all explicitly scoped to Story 10.2 / a follow-up: (a) server-side `token_budget` → `maxResults` forwarding + `omitted_count` response field, (b) `degraded: true` response annotations when a backend is unavailable, (c) ingress authentication (NFR11) via `AddAuthentication("Bearer")` + `AddMcp()` + `AddAuthorizationFilters()`, (d) MCP-specific trace-hop assertion in `AspireEndToEndTraceTests`, (e) **audit stateless-mode (`WithHttpTransport(o => o.Stateless = true)`) compatibility with the 10.2 auth design** — bearer-only flows are stateless-safe, but OAuth-PKCE or session-based refresh flows would require flipping to stateful mode and wiring `ConfigureSessionOptions`. Document the audit trigger so the 10.2 author does not inherit the stateless choice without revisiting it.
  - [x] 10.3 Update `sprint-status.yaml`: `epic-10 backlog → in-progress`; `10-1-mcp-server-and-tool-registration ready-for-dev → in-progress → review → done` across the implementation lifecycle (only `ready-for-dev` flipped by this story-creation step).
  - [x] 10.5 Add NEW `src/Hexalith.Memories.Mcp/README.md` (bundled in the NuGet package so consumers see it on nuget.org) with the same bold unauth warning: "⚠️ **UNAUTHENTICATED in 10.1** — this package exposes `/mcp` without ingress authentication. Run only under `ASPNETCORE_ENVIRONMENT=Development` unless `MEMORIES_MCP_ALLOW_UNAUTHENTICATED=true` is explicitly set. Story 10.2 adds bearer auth." Reference `docs/dev/mcp-server.md` for details. This complements Task 3.6's runtime log — developers see the warning at package install time, operators see it in logs at startup.
  - [x] 10.4 Retrospective entry under `_bmad-output/implementation-artifacts/review-10-1/` on completion.

## Dev Notes

### Architecture alignment

- **Project layout:** `src/Hexalith.Memories.Mcp/` per architecture.md §Structure Patterns (`Hexalith.Memories.Mcp/` listed as row 7 of the 10-package structure). `Microsoft.NET.Sdk.Web` because MCP uses the Streamable HTTP transport. `IsPackable = true` per §Build Order Aligned to Gates.
- **Boundaries:** MCP → Server via DAPR service invocation ONLY (architecture.md §Service Boundaries row "MCP Server (Phase 1.5)"). No direct Redis/FalkorDB access; no secret-store reference; no DAPR state-store reference; no pub/sub subscription. This is NFR11 + architecture §Cross-Cutting Concerns #4 (secret scoping).
- **Error format:** Reuse the Story 6.x `ErrorResponse(Code, Message, Suggestion)` envelope. Do NOT introduce an MCP-specific error type. `McpErrorMapper` is the single translation point.
- **JSON serialization:** `MemoriesJsonContext.Options` everywhere. The ModelContextProtocol SDK has its own serializer defaults for tool arguments — those are fine to leave untouched; the conflict-free boundary is "SDK handles MCP protocol framing; our code handles Server DTO framing".
- **Sidecar ports (3600/50101):** these are **local-dev convenience values** chosen to avoid collision with the Memories Server sidecar (3500/50001). They are NOT a hard contract — under Aspire Testing the CommunityToolkit sidecar provisioning may randomize ports, and the MCP server resolves them via `DAPR_HTTP_ENDPOINT` env → `DaprClient.CreateInvokeHttpClient`. If a future story (e.g., a multi-tenant dev cluster) needs to move these to randomized ports everywhere, it is a one-line change in `AppHost/Program.cs` and does not break consumers. Document the pinning rationale in `docs/dev/mcp-server.md` under "Local dev ports".
- **`[Experimental("HXL003")]` scope:** the diagnostic ID reserves all 10.1-introduced surface whose signature may reshape in 10.2. Apply it to (a) `MemoriesClient.TraverseAsync` + `GetCaseAsync` (Task 2), AND (b) the 4 MCP tool method implementations (`SearchMemoryTool.SearchAsync`, `IngestContentTool.IngestAsync`, `TraverseRelationsTool.TraverseAsync`, `GetCaseInfoTool.GetCaseAsync`) since their parameter surfaces (`token_budget`, `graph_scope`, eventual `degraded` flags) are slated for 10.2 reshape. Suppress with `#pragma warning disable HXL003` at individual call sites in internal callers (e.g., the AppHost composition or test projects) — do NOT suppress globally in `Directory.Build.props`. The schema emitted by the SDK is not affected by `[Experimental]`; it is a compile-time analyzer contract, not a runtime concern.

### Input validation and token-budget handling

The MCP endpoint is the LLM-agent-facing surface. LLM-generated tool calls can contain hallucinated parameter values (e.g., `maxResults = int.MaxValue`, negative numbers, `edge_type` strings with invalid enum names). The server rejects these at its own input-validation layer, but a forwarded call wastes a sidecar hop + Server request-processing cycles per hallucination. 10.1 therefore applies **client-side clamps / early-reject gates** inside each tool method, before forwarding:

| Tool | Parameter | Client-side gate |
|---|---|---|
| `search_memory` | `maxResults` | `Math.Clamp(maxResults, 1, 100)` (silent) |
| `search_memory` | `token_budget` | if set, narrows `maxResults` per the soft-clamp formula below (silent) |
| `search_memory` | `axes` | typed `SearchAxis` enum — invalid strings fail at SDK deserialization (reject) |
| `traverse_relations` | `depth` | `Math.Clamp(depth, 0, 10)` (silent; mirrors server-side clamp) |
| `traverse_relations` | `edgeType` | split on comma and `EdgeType.TryParse` each; on any failure `McpErrorMapper.MapValidation("INVALID_EDGE_TYPE", ...)` without calling the server (reject) |

Clamps are SILENT (clamp to the bound and proceed). Reject-gates are EXPLICIT (`CallToolResult { IsError = true }` with code `INVALID_INPUT` and a suggestion listing valid values). Rationale: clamps are for "too much of a good thing" (LLM that doesn't understand the range); rejects are for "this literally cannot work" (LLM that hallucinated a value from a different enum). Test coverage: Task 8.1–8.4 cover clamp-at-bound and reject-on-invalid per-tool.

**`token_budget` soft clamp — 10.1 vs 10.2 split:**

10.1 ships the `token_budget` integer parameter as part of the `search_memory` tool schema (AC #3) so that LLM agents see a stable tool shape from day one and do not need a second round-trip after 10.2 lands. The server's `/api/search` endpoint does not honor a `tokenBudget` query parameter yet, so **10.1 does NOT forward the value to the server** — but it does NOT silently drop it either.

If the caller supplies `token_budget`, the tool method narrows `maxResults` client-side via `clampedMaxResults = Math.Min(maxResults, Math.Max(1, token_budget / EstimatedTokensPerResult))` where `EstimatedTokensPerResult = 500` is a named constant on `SearchMemoryTool` (NOT a magic number in the method body — see Task 4.1). The estimate is a conservative 10.1 default covering typical memory-unit payloads (snippet + metadata). The tool forwards the clamped `maxResults` to the server, so LLM clients that rely on `token_budget` see real behavior — fewer results — rather than a silently-ignored parameter.

**Documented exposure:** the 500-token estimate is surfaced in three places: (a) the `token_budget` parameter's `[Description]` attribute on `SearchMemoryTool.SearchAsync`, (b) `docs/dev/mcp-server.md` Operations section, (c) the `EstimatedTokensPerResult` const itself with an XML comment. An LLM reading the tool schema sees the estimate and can size its `token_budget` requests accordingly.

**10.2 hand-off seam:** when 10.2 ships, the server-side `token_budget` → `maxResults` translator lands in `Server/Program.cs`'s `/api/search` endpoint + a new `omitted_count` response field appears in `SearchResult`. The MCP tool method then stops applying the client-side soft clamp and starts forwarding `token_budget` as a query parameter. The tool schema doesn't change. LLM clients don't need to re-read the schema.

### Graph scope parameter simplification

Epic AC #4 says `graph_scope (object, optional)` for `traverse_relations`. The current server endpoint (`GET /api/tenants/{tenantId}/traverse`) accepts only `caseId` + `edgeTypes` (comma-separated) as query parameters. Introducing a complex-object `graph_scope = { caseId, ... }` parameter into the MCP tool schema would:

1. Require the LLM to construct a nested JSON object per-call — more tokens, more failure modes, lower tool-call success rate.
2. Create a mismatch between the declared schema (object) and the actual pass-through (two flat strings).

10.1 therefore flattens `graph_scope` into `caseId` (string, optional). When the server grows richer graph-scope semantics (e.g., `sourceFilter`, `tenantCrossReference`), the MCP tool schema expands at that point. Document this in `docs/dev/mcp-server.md` so downstream agent authors are not surprised.

### Tool return types — string vs typed

All 4 tool methods return `string` (JSON-serialized payload) rather than typed `record` or `SearchResult` / `TraversalResult` / `Case` returns. Rationale, in order of strength:

1. **Explicit serialization control (primary):** All tool responses use `MemoriesJsonContext.Options` (camelCase, source-generated). A typed return would let the SDK pick its own serializer (property-naming, converter set), creating wire-format drift across MCP tools vs. the REST surface that consumers already integrate against. Returning `string` puts serialization under our control at a single seam (`McpToolResultSerializer.Serialize`).
2. **Error-path symmetry:** Error tool results are `CallToolResult { IsError = true, Content = [TextContentBlock { Text = "..." }] }`. Success tool results should mirror that shape — `TextContentBlock` with JSON payload — for predictable LLM parsing (the LLM always finds the payload at `content[0].text`).
3. **Future-proof for AOT:** `ModelContextProtocol` 1.2.0 auto-schema-generates the return-type shape. If the tool returns a typed record, the SDK walks the type's schema via reflection — which is AOT-fragile. Returning `string` short-circuits this. 10.1 does NOT enable `<PublishAot>true</PublishAot>` (Risk #2), so this is speculative today, but it costs nothing now and pre-empts a refactor if a future story flips the flag.

### Why DAPR service invocation over direct HttpClient

A reasonable question from a fresh reader: *the MCP Server doesn't handle secrets, doesn't publish events, and has no state store — why force every call through a DAPR sidecar hop?*

The answer has three layers, in order of weight:

1. **Secret scoping (NFR11 + architecture §Cross-Cutting Concerns #4):** DAPR's secret-store scoping restricts which `app-id` can resolve which secret. The Memories Server has access to embedding-provider API keys (OpenAI, Cohere) via `DAPR_SECRET_STORE_*` bindings. The MCP Server app-id is intentionally NOT listed in the secret scope. Routing all cross-service calls through the sidecar makes this the *only* pathway — a direct `HttpClient` would bypass the scope model and invite a future code change to inject secrets into MCP directly. Forcing the sidecar hop preserves the invariant.

2. **Story 5.4 AC3 parity (`DAPR_API_TOKEN_MODE`):** when token mode is enabled, the sidecar enforces `dapr-api-token` on inbound requests. A direct HTTP call skips this gate. Using service-invocation ensures MCP inherits the same security posture as the CLI and ingress — no service-specific exception.

3. **Operational uniformity:** the Aspire Dashboard, distributed-trace view, and resilience policies all assume sidecar traversal. Direct HTTP creates a service-pair that doesn't look like any other service-pair in the topology — a cognitive load tax for operators and an audit-trail gap for compliance.

Costs: one extra network hop (localhost sidecar → localhost sidecar, sub-millisecond); one extra pair of spans in every trace; marginally more complex DI wiring. All acceptable given the benefits above.

**Future consideration:** if the MCP Server and Memories Server ever consolidate (unlikely — they have different auth postures and scaling profiles), the DAPR hop can collapse to an in-process call. Nothing in 10.1 blocks that refactor.

### Startup environment gate (AC 11)

10.1 ships an unauthenticated `/mcp` endpoint. The docs + README warnings (Tasks 10.1, 10.5) address the humans-reading-docs vector. The startup environment gate (Task 3.6, AC 11) addresses the humans-misconfiguring-YAML vector — a layered defense.

**Gate logic:**

```csharp
internal static class McpUnauthenticatedStartupGuard
{
    public static void Validate(IHostEnvironment env, IConfiguration cfg)
    {
        if (env.IsDevelopment()) return;
        if (string.Equals(cfg["MEMORIES_MCP_ALLOW_UNAUTHENTICATED"], "true", StringComparison.OrdinalIgnoreCase)) return;
        throw new InvalidOperationException(
            "MCP 10.1 is unauthenticated. Set MEMORIES_MCP_ALLOW_UNAUTHENTICATED=true " +
            "to run outside Development, or wait for Story 10.2 (bearer auth).");
    }

    public static void LogStartupWarning(ILogger logger) =>
        logger.LogWarning(
            "MCP endpoint /mcp is UNAUTHENTICATED (10.1). Do not expose outside a trusted network. Story 10.2 adds ingress auth.");
}
```

**Call sites:** `Validate` runs in `Program.cs` immediately after `WebApplication.CreateBuilder(args)` (before any service registration — fail-fast) and `LogStartupWarning` runs after `app.MapMcp()` but before `app.Run()` (so the warning lands in operator logs at every startup, not just misconfigurations).

**Opt-out mechanism:** `MEMORIES_MCP_ALLOW_UNAUTHENTICATED=true` is a deliberate escape hatch for staging/test environments where unauth MCP is intentional. It is NOT the correct flag for production — production gets 10.2. The flag's existence is itself a documented signal that 10.1 must not reach production unaltered.

**Removal in 10.2:** when 10.2 wires ingress auth, delete the guard entirely (NOT "leave it there in case"). The guard is a 10.1-specific safety net; keeping it beyond 10.2 invites confusion. Task 10.2 deferred-work entry flags this.

### Dapr invocation handler — keep or delete

Task 3.7 evaluates whether `MemoriesMcpDaprInvocationHandler` is worth its own class. The `DelegatingHandler` adds complexity (one class, 3 unit tests, pipeline registration) to do three things: `dapr-app-id` header, `dapr-api-token` header, and `traceparent` propagation.

- **`dapr-app-id`:** `DaprClient.CreateInvokeHttpClient` already sets this.
- **`traceparent`:** `HttpClient`'s default OTel instrumentation already adds this.
- **`dapr-api-token`:** THE only remaining unique responsibility.

If the token header can be set once at DI time via `HttpClient.DefaultRequestHeaders.Add("dapr-api-token", tokenValue)`, the handler class becomes dead weight. The catch: `DefaultRequestHeaders` is set once — if `DAPR_API_TOKEN_MODE` flips at runtime, the header is stale. But 10.1 treats token mode as startup-fixed (not hot-reloaded), so the static `DefaultRequestHeaders` path is sufficient.

**Decision rule:** if Task 3.7's evaluation confirms token mode is startup-fixed, delete the class + tests. Otherwise, keep it as documented in the original spec. Either way, document the decision and the rationale in the completed story's retrospective.

### DAPR service invocation — preferred `DaprClient.CreateInvokeHttpClient`

The MCP Server uses `DaprClient.CreateInvokeHttpClient("memories-server")` (Dapr.Client 1.17.6 API) to obtain a pre-configured `HttpClient` that routes all requests through the local DAPR sidecar's service-invocation endpoint. This handles:

- Sidecar URL resolution (env var `DAPR_HTTP_ENDPOINT`, defaults to `http://localhost:3500`).
- `dapr-app-id` header injection (set to `memories-server`).
- `dapr-api-token` header injection when the env var is set.

Do NOT hand-roll `HttpClient.BaseAddress = "http://localhost:3500/v1.0/invoke/memories-server/method/"`. Under Aspire Testing the sidecar HTTP port is dynamic; relying on the SDK handler means tests just work.

#### DI shape for `MemoriesClient` (Task 3.2 — concrete recipe)

The `DaprClient.CreateInvokeHttpClient(...)` factory returns a fully-configured `HttpClient`, not a `HttpClientHandler`. `AddHttpClient<TClient>(cfg)` therefore does NOT compose cleanly — its pipeline replaces the inner handler and defeats the Dapr factory's wiring. Use the **singleton-factory** shape instead:

```csharp
builder.Services.AddSingleton<DaprClient>(_ => new DaprClientBuilder().Build());
builder.Services.AddTransient<MemoriesMcpDaprInvocationHandler>();
builder.Services.AddScoped<MemoriesClient>(sp =>
{
    // DaprClient.CreateInvokeHttpClient resolves DAPR_HTTP_ENDPOINT + adds dapr-app-id.
    HttpClient invokeClient = DaprClient.CreateInvokeHttpClient(appId: "memories-server");
    // Chain the invocation handler as a DelegatingHandler via a fresh HttpClient wrap
    // if token injection is needed; otherwise use invokeClient directly.
    return new MemoriesClient(invokeClient, sp.GetRequiredService<IOptions<MemoriesClientOptions>>());
});
```

Scope `MemoriesClient` **scoped** (matches the ASP.NET Core request scope the MCP endpoint runs under) so per-request diagnostics (e.g., `MemoriesActivitySource`) flow naturally. Do NOT register singleton — the Dapr handler may carry request-scoped state in 1.17.6+.

### `CancellationToken` handling in tool methods

Each tool's outer `try`/`catch` wrapper (Task 4.8) must **propagate `OperationCanceledException`** rather than mapping it to `CallToolResult { IsError = true }`. Rationale:

1. `OperationCanceledException` originates from the MCP client (via the inbound `CancellationToken` flowed into `CallToolAsync`) — the client already knows it cancelled.
2. Mapping it to a tool-level error confuses the LLM: it sees "the tool failed" when the caller actually cancelled. That invites retry loops on user-initiated cancels.
3. The MCP SDK handles cancelled calls at the protocol layer by abandoning the response — no structured result is needed.

Shape:

```csharp
try
{
    return await _client.SearchAsync(...);
}
catch (OperationCanceledException)
{
    throw; // let the SDK observe cancellation; do NOT map.
}
catch (MemoriesRemoteException ex)
{
    return _mapper.Map(ex, "search_memory");
}
catch (Exception ex)
{
    return _mapper.MapGeneric(ex, "search_memory");
}
```

### HTTP trace context propagation

`HttpClient`'s default OTel instrumentation (registered via `ServiceDefaults.ConfigureOpenTelemetry`) auto-injects the `traceparent` header on outbound requests from the current `Activity`. When the MCP Server calls the Memories Server via the DAPR invocation hop, the sidecar preserves the header. Story 7.5 / 8.4 trace continuity therefore extends from the MCP endpoint through to the Memories Server with no code changes — this is why the MCP project inherits `ServiceDefaults` rather than rolling its own telemetry.

The 8.4 `AspireEndToEndTraceTests` suite does NOT yet include an MCP hop. Adding that is out-of-scope for 10.1 (deferred to a follow-up story once 10.2 auth lands). But the MCP Server's spans will naturally appear in the Aspire Dashboard's distributed-trace view from day one because `AddAspNetCoreInstrumentation` captures the inbound `/mcp` request.

### ActivitySource for MCP tool execution

`MemoriesActivitySource` (from `Hexalith.Memories.Telemetry`) is shared across Server + CLI + now MCP. The MCP tool methods do NOT need to start their own `Activity` manually — the inbound `/mcp` HTTP span + the outbound `HttpClient` span to the Memories Server is sufficient trace coverage for 10.1. If a follow-up story needs per-tool custom attributes (e.g., `mcp.tool.name = search_memory`), it adds a `MemoriesActivitySource.Instance.StartActivity("mcp.tool.call")` wrapper inside each tool method. 10.1 does not, to keep the surface minimal.

### Stateless mode — rationale

`WithHttpTransport(options => options.Stateless = true)` per the ModelContextProtocol docs §"Host Streamable HTTP MCP Server (ASP.NET Core)" note: "Recommended for stateless operation to simplify deployment and enable horizontal scaling." Story 10.1 has no use case for server-to-client requests (sampling, elicitation) — every tool is pure request/response delegation to the Memories Server. Stateless mode means:

- No in-memory session store — restart-safe.
- Horizontal scaling works trivially — multiple `memories-mcp` replicas share no state.
- `ConfigureSessionOptions` hook is not used (but remains available for 10.2 if auth grows per-request state).

**10.2 interaction — audit before inheriting:** when 10.2 wires ingress authentication, the choice of auth flow determines whether stateless is still the right call. Bearer-only (validate-per-request) flows are stateless-safe. OAuth-PKCE / session-based refresh flows are NOT — they need server-side session state for code/token exchange. Do NOT inherit the 10.1 `Stateless = true` setting into 10.2 without re-evaluating it. `deferred-work.md` carries an explicit entry covering this audit (Task 10.2.e).

### Testing tier alignment

- **Tier 1 (Contracts.Tests + Mcp.Tests/McpToolSchemaTests):** tool schema shape — names, descriptions, types, required fields.
- **Tier 2 (Mcp.Tests tool tests):** stub `MemoriesClient` via `NSubstitute`, assert delegation + error mapping.
- **Tier 3 (IntegrationTests/Mcp):** Aspire fixture, real DAPR sidecars, real Memories Server, real MCP client. Asserts end-to-end trace continuity + DAPR hop works.

This matches Story 1.5 + 6.1 + 8.4 tier splits.

### File-location summary (where to put code)

| Concern | Location |
|---|---|
| MCP project composition root | `src/Hexalith.Memories.Mcp/Program.cs` |
| Tool classes | `src/Hexalith.Memories.Mcp/Tools/` |
| Error mapper | `src/Hexalith.Memories.Mcp/McpErrorMapper.cs` |
| Dapr invocation handler | `src/Hexalith.Memories.Mcp/MemoriesMcpDaprInvocationHandler.cs` |
| Enum types (`SearchAxis`) | `src/Hexalith.Memories.Mcp/Tools/SearchAxis.cs` |
| `TraverseAsync` + `GetCaseAsync` extension | `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` (edit) |
| Package version additions | `Directory.Packages.props` (edit) |
| AppHost resource | `src/Hexalith.Memories.AppHost/Program.cs` (edit) |
| Solution registration | `Hexalith.Memories.slnx` (edit) |
| Tier-1/2 tests | `tests/Hexalith.Memories.Mcp.Tests/` |
| Tier-3 tests | `tests/Hexalith.Memories.IntegrationTests/Mcp/` |
| Docs | `docs/dev/mcp-server.md` |

### Rollback plan

If a disaster ships with `memories-mcp` (e.g., sidecar configuration leaks Server credentials, or MCP schema emission blocks AppHost startup):

1. **Single-line revert in AppHost:** comment out or remove the `AddProject<Projects.Hexalith_Memories_Mcp>("memories-mcp")...` block in `src/Hexalith.Memories.AppHost/Program.cs`. All other services continue running unchanged — the MCP surface is additive, not in the critical path of any existing feature.
2. **No data migration consequences:** MCP is pure read-through + ingestion passthrough. There is no MCP-specific persisted state, no DAPR state-store binding, no schema change in Redis or FalkorDB. A revert does NOT require data cleanup.
3. **Consumer impact:** only LLM-agent clients that were already talking to the MCP endpoint (not yet in prod at 10.1). Internal consumers (CLI, Server REST, EventStore) are untouched.
4. **Rollback window:** same release cycle — no NuGet package to yank (the MCP package is new in this sprint; no external consumer is pinned yet).
5. **Re-land path:** address the root cause, re-add the AppHost block, re-ship. No architectural reshape required.

Document this rollback plan in `docs/dev/mcp-server.md` under "Operator rollback" so on-call engineers can execute it without waiting for a developer.

### Project Structure Notes

- Aligns with architecture.md §Structure Patterns (`src/Hexalith.Memories.Mcp/` at row 7 of 10 projects) and §Build Order Aligned to Gates (row 9, Phase 1.5).
- No conflicts or variances. The new package follows the `Hexalith.Memories.EventStore.csproj` template verbatim (same license, author, package metadata shape). The slnx registration sits alphabetically adjacent to EventStore + Redis.
- Directory.Packages.props adds 2 new entries — central-package-versioning compliant.
- Tests project `Hexalith.Memories.Mcp.Tests` mirrors the `Hexalith.Memories.EventStore.Tests` naming convention.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic-10-MCP-Server-and-LLM-Agent-Interface] Story 10.1 ACs, Epic 10 objectives.
- [Source: _bmad-output/planning-artifacts/epics.md#FR54-FR58] Functional requirements for the 4 tools + typed schemas.
- [Source: _bmad-output/planning-artifacts/epics.md#NFR20] MCP protocol conformance.
- [Source: _bmad-output/planning-artifacts/architecture.md#Service-Boundaries] MCP Server → Memories Server via DAPR service invocation.
- [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Cutting-Concerns] DAPR Secrets scoping — MCP has no direct secret access.
- [Source: _bmad-output/planning-artifacts/architecture.md#Build-Order-Aligned-to-Gates] `Hexalith.Memories.Mcp` as project 9, Phase 1.5.
- [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries] API boundaries table row "MCP (Phase 1.5)" — delegates to Server via Client.
- [Source: src/Hexalith.Memories.AppHost/Program.cs:67-90] `WithDaprSidecar` + `WaitFor` pattern to mirror.
- [Source: src/Hexalith.Memories.Server/Program.cs:2039,2773,1458] REST endpoints for `/api/search`, `/api/tenants/{tenantId}/traverse`, `/api/tenants/{tenantId}/cases/{caseId}` that MCP tools delegate to.
- [Source: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:267,341,402,586] `[Experimental("HXL001")]` precedent — 10.1 reserves `HXL003`.
- [Source: src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs] AOT-safe JSON serialization context — reuse for Server DTO framing.
- [Source: src/Hexalith.Memories.ServiceDefaults/Extensions.cs:75-113] OpenTelemetry + Redis OTEL + HTTP resilience configuration inherited by MCP.
- [Source: src/Hexalith.Memories.EventStore/Hexalith.Memories.EventStore.csproj] Publishable-project csproj template.
- [Source: _bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md] Precedent for AppHost-wiring + DAPR-sidecar + adapter-interface package pattern.
- [Source: tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs] Fixture pattern for `DistributedApplicationTestingBuilder` + `app.GetEndpoint(...)` + peer-service trace-span assertions — copy the shape verbatim for `tests/Hexalith.Memories.IntegrationTests/Mcp/McpServerIntegrationTests.cs`.
- [Source: ModelContextProtocol C# SDK 1.2.0 (github.com/modelcontextprotocol/csharp-sdk)] `AddMcpServer`, `WithHttpTransport`, `WithTools<T>`, `[McpServerTool]`, `[McpServerToolType]`, `[Description]`, `CallToolResult`, `IsError`, `TextContentBlock`, stateless-mode rationale, error-handling via `CallToolResult` rather than thrown exceptions.

### Review Findings

- [x] [Review][Decision] `search_memory` advertises `Graph` but has no way to provide `startNodeId` — resolved by omitting `Graph` from the `search_memory` `SearchAxis` enum/schema in 10.1 and directing graph operations to `traverse_relations`.
- [x] [Review][Patch] Tool error paths are serialized as JSON strings, so MCP clients will not receive protocol-level `IsError = true` [src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs:60] — fixed by returning protocol-level `CallToolResult` from tool methods; success payloads still carry Memories JSON in `content[0].text` and `structuredContent`.
- [x] [Review][Patch] `MemoriesServerUpstreamHealthCheck` is registered as singleton while depending on scoped `MemoriesClient` [src/Hexalith.Memories.Mcp/McpCompositionRoot.cs:38] — fixed by resolving `MemoriesClient` inside a health-check scope per probe.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.7 (1M context).

### Debug Log References

- Initial restore failure on `OpenTelemetry.Exporter.OpenTelemetryProtocol 1.15.1` advisories
  (GHSA-mr8r-92fq-pj8p, GHSA-q834-8qmm-v933) blocked builds across the repo. Resolved by bumping
  the OTel core/exporter/extensions stack 1.15.1 → 1.15.3 (`OpenTelemetry`,
  `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`,
  `OpenTelemetry.Exporter.InMemory`). `OpenTelemetry.Instrumentation.AspNetCore` only has 1.15.2
  published (no 1.15.3) and was not flagged by the advisories, so it ships at 1.15.2 — tracked in
  `deferred-work.md` (`Story-10.x-OpenTelemetryAspNetCoreAlignment`) so a future patch run can
  re-align all OTel pins on the same point release.
- ModelContextProtocol package version 1.2.0 confirmed against nuget.org listing (latest stable
  Microsoft-collaboration release; 0.x line is preview only).
- MCP SDK 1.2.0 emits PascalCase enum literals in tool schemas (`Syntactic`, `Semantic`,
  `Hybrid`) regardless of the `[JsonConverter(typeof(CamelCaseStringEnumConverter<>))]` attached
  to the enum. Wire-level deserialization is case-insensitive so the runtime contract still
  works; the contract test asserts the four canonical axes case-insensitively. Documented in
  `docs/dev/mcp-server.md`.
- Task 3.7 evaluation outcome: `MemoriesMcpDaprInvocationHandler` collapsed from a full
  `DelegatingHandler` to a static helper (`ApplyDaprApiToken`) because (a) `dapr-app-id` is
  injected by `DaprClient.CreateInvokeHttpClient`, (b) `traceparent` is added by `HttpClient`'s
  default OTel instrumentation, and (c) `DAPR_API_TOKEN_MODE` is startup-fixed in 10.1 so static
  `HttpClient.DefaultRequestHeaders.Add` is sufficient.
- Task 3.5 DI-shape spike outcome: singleton-factory shape (`AddScoped<MemoriesClient>` resolving
  via `DaprClient.CreateInvokeHttpClient` + `MemoriesMcpDaprInvocationHandler.ApplyDaprApiToken`)
  preferred over `AddHttpClient<TClient>` typed-client because the Dapr factory returns a
  fully-configured `HttpClient` whose pipeline is replaced by `IHttpClientFactory`-based wiring,
  defeating the sidecar URL resolution.
- Task 9 fixture extension: `AspireIngestionPipelineFixture` now waits for the `memories-mcp`
  resource healthy and exposes `McpClient` + `McpEndpoint`. Tier-3 tests use the
  `HttpClientTransport` (mode = `StreamableHttp`) on `McpClient.CreateAsync(...)` from
  `ModelContextProtocol.Client`. Docker-gated; runs on the Integration lane.

### Completion Notes List

- New `src/Hexalith.Memories.Mcp/` project (Microsoft.NET.Sdk.Web, IsPackable=true) ships with
  the four FR54 tools — `search_memory`, `ingest_content`, `traverse_relations`,
  `get_case_info` — registered via `AddMcpServer().WithHttpTransport(o => o.Stateless = true)`.
- Tools delegate exclusively through `MemoriesClient` over `DaprClient.CreateInvokeHttpClient`
  service invocation (NFR11). Two new `[Experimental("HXL003")]` methods on `MemoriesClient` —
  `TraverseAsync` + `GetCaseAsync` — close the only client-side gaps (Task 2). HXL003 is
  suppressed at individual call sites in MCP tools and tests (no project-level `<NoWarn>`).
- Error mapping flows through a single seam: `McpErrorMapper.{Map, MapGeneric, MapValidation}`.
  `MapGeneric` sanitizes the message — no stack traces, no echo of caller-supplied payloads
  (path traversal strings, SQL fragments, oversized payloads) — keeping the LLM-facing surface
  free of input-leak channels. The `tool` field in `StructuredContent` is always the literal
  tool name passed in, never derived from user input (security gate).
- Client-side guards mirror the server's input validation: `maxResults` clamped to `[1, 100]`
  silently, `depth` clamped to `[0, 10]` silently, invalid `edgeType` values reject client-side
  with `INVALID_EDGE_TYPE` before any sidecar hop. `tokenBudget` (10.1 schema-stable, server-side
  truncation deferred to 10.2) narrows `maxResults` via the conservative
  `EstimatedTokensPerResult = 500` heuristic; the constant is referenced from tests so impl + test
  stay in lockstep.
- `McpUnauthenticatedStartupGuard.Validate` fails fast at startup outside Development unless
  `MEMORIES_MCP_ALLOW_UNAUTHENTICATED=true` (AC #11); the warning log is emitted on every
  successful startup so operators see the unauth posture in `kubectl logs`.
- `AppHost/Program.cs` wires `memories-mcp` as a sibling DAPR resource (sidecar AppId
  `memories-mcp`, ports 3600/50101) with `WaitFor(server)`. No `WithReference(stateStore | pubSub
  | secretStore | conversationLlm)` — the secret-scope invariant from architecture
  §Cross-Cutting Concerns #4 is preserved. `APP_API_TOKEN` / `DAPR_API_TOKEN` env vars propagate
  to the MCP resource when token mode is enabled, mirroring the Server resource exactly.
- Health checks: `DaprSidecarHealthCheck` (live + ready) duplicated locally because the MCP
  project cannot reference the Server project; `MemoriesServerUpstreamHealthCheck` (3-strike
  rolling-window, Degraded → Unhealthy escalation, `data["upstream"] = "memories-server"`).
  `MapDefaultEndpoints()` from `ServiceDefaults` exposes `/health`, `/alive`, `/ready`.
- Test coverage:
  - **Tier-1 contract tests (8 tests)** — `McpToolSchemaTests`: 4 distinct tool names, every
    parameter has non-trivial prose description (FR58 enforcement), `axes` enum has 3 values,
    `tokenBudget` schema-stable, `query`/`tenantId` required, `traverse_relations` `caseId` is
    a flat string (no `graphScope` object).
  - **Tier-2 unit tests (48 tests)** — `SearchMemoryToolTests` (10), `IngestContentToolTests`
    (5), `TraverseRelationsToolTests` (5), `GetCaseInfoToolTests` (2),
    `McpErrorMapperTests` (12 incl. all security gates from Risk #4),
    `MemoriesMcpDaprInvocationHandlerTests` (3 — AC #8 token-header guard),
    `McpUnauthenticatedStartupGuardTests` (4 — AC #11 startup-gate guard).
  - **Tier-2 client tests (6 tests)** — `MemoriesClientTraverseTests` covers the new
    `TraverseAsync` + `GetCaseAsync` happy paths, query shapes, 404 + INVALID_RESPONSE errors.
  - **Tier-3 integration tests (2 tests, Category=Integration)** —
    `McpServerIntegrationTests`: `ListTools_EndToEnd_ReturnsFourToolsWithTypedSchemas` +
    `CallSearchMemory_EndToEnd_ExecutesAcrossDaprHop`. Docker-gated (skipped on per-PR runs).
  - Total Story 10.1 new tests: **61 unit/contract + 2 integration = 63**.
- Final validation: solution builds 0 W / 0 E. `Hexalith.Memories.Mcp.Tests` 55/55,
  `MemoriesClientTraverseTests` 6/6, `Cli.Tests` 322/322 (full), `Contracts.Tests` 334/334,
  `EventStore.Tests` 84/84, `Server.Tests` 1491/1494 — the 3 failures
  (`IngestionInputValidatorTests.Validate_Event_WithNullBytes_Throws`,
  `DocumentationCompletenessTests.EventStoreIntegrationDoc_HasRequiredSectionsAndKeyContent`,
  `ProvisionRediSearchActivityTests.RunAsync_IndexAlreadyExistsWithMatchingSchema_ShouldReturnTrue`)
  are pre-existing, reproduce on HEAD, and match the documented baseline from prior sprint-status
  entries (Story 9.2 / 8.5 sessions noted these as "pre-existing failures NOT introduced by this
  story").
- Deferrals to 10.2 / follow-ups documented in `_bmad-output/implementation-artifacts/deferred-work.md`:
  token-budget server truncation, degraded-state annotations, ingress authentication (NFR11),
  MCP-specific trace-hop assertion, stateless-mode audit for 10.2 auth, MCP AOT compatibility,
  tokenizer-accurate budget, OTel AspNetCore instrumentation 1.15.3 alignment.

### File List

**New files (under `src/Hexalith.Memories.Mcp/`):**

- `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj`
- `src/Hexalith.Memories.Mcp/README.md`
- `src/Hexalith.Memories.Mcp/Program.cs`
- `src/Hexalith.Memories.Mcp/McpCompositionRoot.cs`
- `src/Hexalith.Memories.Mcp/McpUnauthenticatedStartupGuard.cs`
- `src/Hexalith.Memories.Mcp/McpErrorMapper.cs`
- `src/Hexalith.Memories.Mcp/McpToolResultSerializer.cs`
- `src/Hexalith.Memories.Mcp/MemoriesMcpDaprInvocationHandler.cs`
- `src/Hexalith.Memories.Mcp/Tools/SearchAxis.cs`
- `src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs`
- `src/Hexalith.Memories.Mcp/Tools/IngestContentTool.cs`
- `src/Hexalith.Memories.Mcp/Tools/TraverseRelationsTool.cs`
- `src/Hexalith.Memories.Mcp/Tools/GetCaseInfoTool.cs`
- `src/Hexalith.Memories.Mcp/Health/DaprSidecarHealthCheck.cs`
- `src/Hexalith.Memories.Mcp/Health/MemoriesServerUpstreamHealthCheck.cs`
- `src/Hexalith.Memories.Mcp/appsettings.json`
- `src/Hexalith.Memories.Mcp/appsettings.Development.json`
- `src/Hexalith.Memories.Mcp/Properties/launchSettings.json`

**New test project (`tests/Hexalith.Memories.Mcp.Tests/`):**

- `tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj`
- `tests/Hexalith.Memories.Mcp.Tests/StubMemoriesClient.cs`
- `tests/Hexalith.Memories.Mcp.Tests/McpToolSchemaTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/SearchMemoryToolTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/IngestContentToolTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/TraverseRelationsToolTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/GetCaseInfoToolTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/McpErrorMapperTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/McpUnauthenticatedStartupGuardTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/MemoriesMcpDaprInvocationHandlerTests.cs`

**New integration test (`tests/Hexalith.Memories.IntegrationTests/Mcp/`):**

- `tests/Hexalith.Memories.IntegrationTests/Mcp/McpServerIntegrationTests.cs`

**New client tests:**

- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientTraverseTests.cs`

**New docs:**

- `docs/dev/mcp-server.md`

**Modified files:**

- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` — added `[Experimental("HXL003")]`
  `TraverseAsync` + `GetCaseAsync` + private `CamelCase` helper.
- `src/Hexalith.Memories.AppHost/Program.cs` — added `memories-mcp` `ProjectResource` block
  with own DAPR sidecar (3600/50101), `WaitFor(server)`, token env propagation.
- `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` — added
  `<ProjectReference>` for `Hexalith.Memories.Mcp`.
- `Hexalith.Memories.slnx` — added `Hexalith.Memories.Mcp` and `Hexalith.Memories.Mcp.Tests`
  projects.
- `Directory.Packages.props` — added `ModelContextProtocol` 1.2.0 +
  `ModelContextProtocol.AspNetCore` 1.2.0; bumped `OpenTelemetry`,
  `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`,
  `OpenTelemetry.Exporter.InMemory` 1.15.1 → 1.15.3 to clear NU1902 advisories;
  `OpenTelemetry.Instrumentation.AspNetCore` bumped 1.15.1 → 1.15.2 (no 1.15.3 published).
- `tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj` —
  added `Hexalith.Memories.Mcp` ProjectReference + `ModelContextProtocol` package reference.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs` —
  added `McpClient` + `McpEndpoint` properties + fixture wait/exposure for `memories-mcp`.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — flipped
  `10-1-mcp-server-and-tool-registration` ready-for-dev → in-progress → review.
- `_bmad-output/implementation-artifacts/deferred-work.md` — added 8 Story-10.1 deferred entries.

### Change Log

- **2026-04-25 — Story 10.1 Session 1.** Initial implementation: scaffolded
  `Hexalith.Memories.Mcp` project (Microsoft.NET.Sdk.Web, IsPackable=true) with the four
  registered tools (`search_memory`, `ingest_content`, `traverse_relations`, `get_case_info`),
  centralized `McpErrorMapper`, AC #11 startup guard, `MemoriesServerUpstreamHealthCheck`
  3-strike health probe, AppHost wiring with own DAPR sidecar, AC #8 `dapr-api-token`
  propagation. Task 2 added `[Experimental("HXL003")]` `TraverseAsync` + `GetCaseAsync` to
  `MemoriesClient`. 61 new unit/contract tests (55 MCP + 6 client) green; 2 Tier-3 integration
  tests added (Docker-gated, Category=Integration). Bumped OTel core/exporter/extensions stack
  1.15.1 → 1.15.3 to clear NU1902 advisories that would otherwise block restore. Story status
  ready-for-dev → in-progress → review.
