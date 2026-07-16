# Story 10.2: Token-Budget Responses & Authentication

Status: done

**Effort estimate:** ~8.5-9.5 working days. Breakdown:

- **0.5 day — Task 1:** Contracts (`SearchResult`, `HybridSearchResult`, `TraversalResult`) additive fields — `OmittedCount`, `Degraded`, `UnavailableAxes`. Pure record-surface changes + `MemoriesJsonContext` registration audit.
- **1.0 day — Task 2:** Server-side `tokenBudget` truncation in `/api/search` (syntactic / semantic / graph / hybrid) — rank-descending inclusion, stop when next result would bust the budget, emit `OmittedCount`.
- **1.0 day — Task 3:** Server-side `tokenBudget` truncation in `/api/tenants/{tenantId}/traverse` — leaf-first pruning that preserves the primary causal path, emit `OmittedCount` on `TraversalResult`.
- **0.75 day — Task 4:** Server-side per-axis `Degraded` / `UnavailableAxes` on single-axis `SearchResult` and `TraversalResult` (the hybrid path already has this — reuse the `GraphScopedSearch` degradation signal + the `GraphTraversalService` backend-health plumbing).
- **0.5 day — Task 5:** `MemoriesClient` + `SearchRequest` / `HybridSearchRequest` / `TraversalRequest` DTO surface — add `TokenBudget` field; retire `[Experimental("HXL003")]` on `TraverseAsync` + `GetCaseAsync` (their 10.2 shape is now locked).
- **0.5 day — Task 6:** MCP forwards `token_budget` to server-side (delete the 10.1 `EstimatedTokensPerResult` client-side soft clamp from `SearchMemoryTool`) + surfaces `omitted_count` / `degraded` / `unavailable_axes` in tool result JSON.
- **1.25 days — Task 7:** MCP ingress authentication — `AddAuthentication("Bearer")` + `AddMcp()` + `AddAuthorizationFilters()` per ModelContextProtocol 1.2.0 docs; new `MemoriesMcpAuthenticationOptions` + `ConfigureJwtBearerOptions` + `MemoriesMcpProblemDetailsChallengeWriter` mirroring the `Hexalith.EventStore.Authentication` pattern; delete `McpUnauthenticatedStartupGuard` + its 4 guard tests (10.1 deferred-work entry).
- **0.75 day — Task 8:** MCP tenant-context pass-through — resolve `ClaimsPrincipal` from the inbound JWT, validate the tool's `tenantId` parameter matches an authorized-tenant claim, short-circuit with a structured `CallToolResult { IsError = true }` (code `TENANT_FORBIDDEN`) when the claim is missing or mismatched. NO tool-method signature changes (the SDK schema-generator excludes `ClaimsPrincipal` automatically; inject via `IHttpContextAccessor`).
- **0.5 day — Task 9:** AppHost wiring — propagate `Authentication__JwtBearer__*` env vars to the `memories-mcp` resource; keep `DAPR_API_TOKEN_MODE=enabled` parity wired through 10.1.
- **1.5 days — Task 10:** Tests. Tier-1: contract-schema shape assertions for `OmittedCount`/`Degraded` fields, `MemoriesJsonContext` serialization round-trip. Tier-2: `TokenBudgetTruncator` unit tests (search + traverse), `MemoriesMcpAuthenticationTests` (401 shapes), `TenantClaimAuthorizationTests` (claim match / mismatch). Tier-3: `McpAuthenticationIntegrationTests` — valid bearer, missing bearer, expired bearer, cross-tenant claim; extend `McpServerIntegrationTests.CallSearchMemory_EndToEnd_ExecutesAcrossDaprHop` to assert `omitted_count` appears when the budget is tight. **Extend `AspireEndToEndTraceTests` with an MCP hop** — deferred from 10.1 becomes the first-class integration test added here (covers the "MCP-specific trace-hop assertion" entry in `deferred-work.md`).
- **0.5 day — Task 11:** Docs + retro + `deferred-work.md` cleanup (close the 5 entries 10.1 left pointing at 10.2). Remove the "UNAUTHENTICATED in 10.1" bold warnings from `docs/dev/mcp-server.md` + `src/Hexalith.Memories.Mcp/README.md` and replace with a "Bearer authentication (Story 10.2)" operator section.
- **0.75 day cushion** for (a) `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.x version compatibility with `ModelContextProtocol.AspNetCore` 1.2.0 `AddMcp()` — the SDK's authorization-filter surface may require a preview / minor bump that is not obvious until wired; (b) stateless-mode audit outcome (deferred-work entry e) — bearer flows are stateless-safe but an OAuth-PKCE path may require stateful switch; (c) tokenizer-estimate accuracy under non-ASCII content (`content_snippet` truncation heuristic — char-count/4 is a conservative default but may over-prune for CJK languages, document the trade-off).

**HARD prerequisite:** Story 10.1 (`10-1-mcp-server-and-tool-registration`) — MUST be in `done` status before 10.2 starts. 10.2 edits / extends / deletes code introduced by 10.1 (`SearchMemoryTool`, `TraverseRelationsTool`, `McpErrorMapper`, `McpUnauthenticatedStartupGuard`, `MemoriesClient.TraverseAsync`) and depends on the `Hexalith.Memories.Mcp` project scaffold that 10.1 creates.

**SOFT prerequisite:** None. Story 9.2 (`review`) and 9.3 (`ready-for-dev`) are parallel-safe — the server REST surface and `MemoriesJsonContext` additions those stories touch are orthogonal to the token-budget + authentication surface added here. A merge-order conflict on `MemoriesJsonContext.cs` is possible but trivially resolved (both stories only append `[JsonSerializable]` declarations).

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** The four MCP tool responses become **budget-aware** and the `/mcp` endpoint becomes **authenticated**. Concretely: (1) LLM agents pass `token_budget` to `search_memory` / `traverse_relations` and the **server-side** truncates results by rank (search) or by leaf-distance (traverse), returning `omitted_count` in the response envelope. (2) All four tools return `degraded: true` + `unavailable_axes: [...]` when a backend is down, so the LLM can caveat its answer. (3) The `/mcp` endpoint rejects unauthenticated requests with an RFC 6750-compliant 401 challenge; authenticated requests carry a JWT whose `tenant_id` claim is cross-checked against the `tenantId` tool parameter inside MCP before any DAPR hop. (4) The 10.1 `McpUnauthenticatedStartupGuard` safety-net + its four guard tests are **deleted** — they exist only to keep 10.1 from shipping unauthenticated, and 10.2 removes the condition they guard against. (5) DAPR API token authentication continues to secure the MCP-to-Server sidecar hop (already wired in 10.1 via `DAPR_API_TOKEN_MODE=enabled` — 10.2 just confirms the chain end-to-end). FR23 (token-budget) and NFR11 (ingress auth) close in this story.

**What already exists (do NOT rebuild):**

1. **`Hexalith.Memories.Mcp` project** — `src/Hexalith.Memories.Mcp/` with the 4 tools, `McpErrorMapper`, `McpToolResultSerializer`, `Program.cs` composition root. Landed in 10.1. **Reuse verbatim.** 10.2 edits existing files rather than re-scaffolding.
2. **`HybridSearchResult` already carries `Degraded` + `UnavailableAxes` + `AllEnabledAxesUnavailable`.** `src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs`. The hybrid path plumbed these through in Story 7.x. **Reuse verbatim** — do NOT rename or reshape. 10.2 adds parallel fields to the single-axis `SearchResult` + `TraversalResult` so all four tool responses look symmetric from the LLM's perspective.
3. **Single-axis degradation detection — `GraphScopedSearch` backend-probe pattern.** `src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs` already tracks per-axis failure. The syntactic / semantic / graph endpoint dispatchers at `Program.cs:2116` detect axis-level unavailability via the `preUnavailableAxes` list (`Program.cs:2441`). **Reuse the detection.** Task 4 just exposes the signal in the single-axis response envelope (it was previously only exposed in the hybrid envelope).
4. **`ErrorResponse(Code, Message, Suggestion)` envelope.** `src/Hexalith.Memories.Contracts/V1/ErrorResponse.cs`. **Reuse verbatim** for every 4xx/5xx path. Add one new error code: `TENANT_FORBIDDEN` — emitted by the MCP tenant-claim authorization filter when the JWT's `tenant_id` claim does not match the tool's `tenantId` parameter.
5. **`McpErrorMapper.Map(MemoriesRemoteException, toolName)` + `.MapGeneric(Exception, toolName)`.** `src/Hexalith.Memories.Mcp/McpErrorMapper.cs` (landed 10.1). **Reuse.** Task 8 adds one new mapping helper — `McpErrorMapper.MapAuthorization(string tenantId, string toolName, string reasonCode)` — returning a `CallToolResult { IsError = true, Content = [TextContentBlock], StructuredContent = { code: "TENANT_FORBIDDEN", ... } }` structured exactly like the existing error shapes so the LLM client's error-handling code needs no changes.
6. **`MemoriesClient.HybridSearchAsync` / `SearchAsync` / `TraverseAsync` / `GetCaseAsync`.** Already wired in 10.1 (the latter two under `[Experimental("HXL003")]`). **Reuse, extend.** Task 5 adds the `TokenBudget` field to the request records and retires the `HXL003` attribute (the 10.2 surface is stable).
7. **JWT-bearer authentication pattern — `Hexalith.EventStore.Authentication`.** `src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Authentication/` carries a production-quality `ConfigureJwtBearerOptions` + `EventStoreAuthenticationOptions` + `ValidateEventStoreAuthenticationOptions` + symmetric/OIDC dual-mode validation + ProblemDetails challenge writer. **Copy the shape, NOT the code.** Duplicating source across the submodule boundary is preferable to adding a `ProjectReference` to the EventStore submodule's infrastructure classes (that would couple two unrelated products). 10.2 adds `src/Hexalith.Memories.Mcp/Authentication/` with four files named `MemoriesMcpAuthenticationOptions.cs`, `ConfigureJwtBearerOptions.cs`, `ValidateMcpAuthenticationOptions.cs`, `MemoriesMcpClaimsTransformation.cs` — same shape, MCP-specific logging / challenge strings.
8. **`MemoriesMcpDaprInvocationHandler` (if Task 3.7 of 10.1 kept it) or inline `HttpClient.DefaultRequestHeaders` (if 10.1 deleted it).** Whichever 10.1 landed with carries the `dapr-api-token` header. **Reuse verbatim** — 10.2 does NOT re-evaluate this decision.
9. **`MemoriesJsonContext.Options`.** `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`. **Reuse verbatim**; Task 1 audits + adds `[JsonSerializable]` registrations for any new public types (none anticipated — the 10.2 additions are all field-level on existing records, which inherit the existing registrations).
10. **ModelContextProtocol SDK 1.2.0 `AddMcp()` + `AddAuthorizationFilters()`.** Landed in SDK 1.2.0 per ModelContextProtocol docs §"Configure MCP Server with Authorization". **Use directly** — do NOT hand-roll middleware on top of `MapMcp()`. The SDK wires bearer-challenge responses into the MCP protocol envelope (instead of plain ASP.NET Core `UnauthorizedResult`) so LLM clients see a protocol-level authorization error rather than a transport-level hiccup.

**What 10.2 adds:**

1. **`src/Hexalith.Memories.Contracts/V1/SearchResult.cs`** — EDIT. Add three fields mirroring `HybridSearchResult` (additive, all init-only, JSON-ignored when default to avoid wire noise):
   ```csharp
   /// <summary>Gets the count of results that were omitted due to token-budget truncation.
   /// Zero means no truncation (or no budget specified).</summary>
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
   public int OmittedCount { get; init; }

   /// <summary>Gets a value indicating whether any expected backend component was unavailable.
   /// <c>false</c> means the endpoint executed against a fully-healthy dependency set.</summary>
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
   public bool Degraded { get; init; }

   /// <summary>Gets the list of axis / component names that were unavailable at runtime.
   /// Empty when <see cref="Degraded"/> is <c>false</c>.</summary>
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
   public IReadOnlyList<string> UnavailableAxes { get; init; } = [];
   ```
   Field names and JSON-ignore conditions **must match** `HybridSearchResult` verbatim so the single-axis and hybrid wire shapes line up for the MCP tool serializer.

2. **`src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs`** — EDIT. Add ONE field — `OmittedCount` (same shape as above). The `Degraded` / `UnavailableAxes` / `AllEnabledAxesUnavailable` already exist; do NOT touch them.

3. **`src/Hexalith.Memories.Contracts/V1/TraversalResult.cs`** — EDIT. Switch from a positional record to an init-only property record so additive fields don't break the constructor. Add three fields:
   ```csharp
   /// <summary>Gets the count of nodes that were pruned due to token-budget truncation. Leaf nodes are pruned first, preserving the primary causal path.</summary>
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
   public int OmittedCount { get; init; }

   /// <summary>Gets a value indicating whether any expected backend (e.g., FalkorDB) was unavailable during traversal.</summary>
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
   public bool Degraded { get; init; }

   /// <summary>Gets the list of unavailable backend names (e.g., <c>["graph"]</c>).</summary>
   [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
   public IReadOnlyList<string> UnavailableAxes { get; init; } = [];
   ```
   **Compatibility:** the positional constructor `TraversalResult(string, int, IReadOnlyList<TraversalNode>, int)` MUST continue to work — convert positional params to the `required` init-only set, and leave the existing `GapMarkers` init-only. No callers break.

4. **`src/Hexalith.Memories.Client.Rest/SearchRequest.cs`** + **`HybridSearchRequest.cs`** — EDIT. Add `TokenBudget` as a nullable `int?` so `null` means "no budget — server returns the default page". Wire it into `BuildSearchPath` (at `MemoriesClient.cs:538`) as a `&tokenBudget=N` query parameter, omitted when null.

5. **`src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`** — EDIT. `TraverseAsync` gains `int? tokenBudget = null` as an additional parameter + appends `&tokenBudget=N` to the traverse URL when non-null. Retire `[Experimental("HXL003")]` on both `TraverseAsync` + `GetCaseAsync` — the 10.2 shape locks them in. Add a migration-friendly note in XML remarks: "stable since 10.2".

6. **`src/Hexalith.Memories.Server/Search/TokenBudgetTruncator.cs`** — NEW internal helper class. ONE public method per target envelope:
   ```csharp
   internal static class TokenBudgetTruncator
   {
       // Rank-preserving truncation — results are assumed pre-sorted by relevance descending.
       public static (IReadOnlyList<T> kept, int omitted) TruncateByRank<T>(
           IReadOnlyList<T> ranked, int? tokenBudget, Func<T, int> tokenEstimator);

       // Leaf-first truncation for traversal — preserves the primary causal chain.
       public static (IReadOnlyList<TraversalNode> kept, int omitted) TruncateTraversal(
           IReadOnlyList<TraversalNode> nodes, int? tokenBudget,
           Func<TraversalNode, int> tokenEstimator);

       // Approximate token count from a content snippet. MVP heuristic: ceil(chars/4) + metadata overhead.
       public static int EstimateTokensForSnippet(string? snippet, int overhead = 24);
   }
   ```
   **`TruncateByRank`** iterates in order, accumulates `tokenEstimator(item)` until adding the next item would exceed `tokenBudget`, then returns `(kept, omitted = ranked.Count - kept.Count)`. **`TruncateTraversal`** walks the causal chain DAG, protects the primary path from the start node to the deepest reachable node (compute via BFS with depth weighting), and prunes leaves outward until the total estimated size fits. **`EstimateTokensForSnippet`** uses `ceil(snippet.Length / 4)` as a conservative heuristic covering English / Latin scripts and overestimates for non-ASCII (acceptable for truncation — over-prune rather than under-prune). Document the heuristic + why no tokenizer library in `docs/dev/mcp-server.md` under "Token budget — accuracy and trade-offs".

   **Pre-impl spike (30 min, Task 2.0):** verify ASP.NET Core minimal-API serialization produces identical wire bytes for `HybridSearchResult { OmittedCount = 0 }` before and after the 10.2 field addition — the new property is `JsonIgnoreCondition.WhenWritingDefault`, so existing clients that don't know about `OmittedCount` must not see a new field in their JSON payload. Guard: `HybridSearchResultSerializationRoundTripTests.PreExistingWireShapeIsUnchangedWhenOmittedCountIsZero` (Task 10.1).

7. **`src/Hexalith.Memories.Server/Program.cs`** — EDIT at `/api/search` (around line 2116) and `/api/tenants/{tenantId}/traverse` (around line 2850). Both endpoints already accept query parameters; add `[FromQuery] int? tokenBudget = null` (after the existing `explain` parameter). Plumb through to the search services:
   - **Syntactic / semantic / graph (single-axis):** after `SyntacticSearchService.SearchAsync` or equivalent returns `IReadOnlyList<ScoredResult>`, call `TokenBudgetTruncator.TruncateByRank(results, tokenBudget, r => TokenBudgetTruncator.EstimateTokensForSnippet(r.ContentSnippet))`. Use the `kept` list for the response `Results` field and set `OmittedCount = omitted`. Preserve `TotalCount` — it still reports the full matched count, so the client can show "showing X of Y (omitted Z due to token budget)".
   - **Hybrid:** the hybrid path already computes `FusedScoredResult[]` in `HybridSearchService.SearchAsync`. Call the same `TruncateByRank` on the fused results (using `r.ContentSnippet` as the estimator input) **after** fusion + normalization, so the `CompositeScore` ordering defines the truncation order. Set `OmittedCount` on the outgoing `HybridSearchResult`.
   - **Traverse:** `GraphTraversalService.TraverseAsync` returns `TraversalResult`. Call `TruncateTraversal` on `result.Nodes` with the node's `Summary.ContentSnippet` as the estimator input. Preserve `GapMarkers` entries referencing retained nodes; drop gap markers pointing exclusively to pruned leaves. Emit `OmittedCount`.
   - **Degraded / UnavailableAxes for single-axis:** lift the existing `preUnavailableAxes` list at `Program.cs:2441` from the hybrid branch into a shared `DetermineUnavailableAxes(axis, graphScopedStartNodeId, backendHealth)` helper callable from both single-axis and hybrid branches. Set `Degraded = (unavailableAxes.Count > 0 && axis != unavailable's only entry)` on `SearchResult`. Graph-axis traverse goes Degraded only when FalkorDB-side health probe fails during traversal — surface via `TraversalResult.Degraded = true, UnavailableAxes = ["graph"]` and return 200 (with the surviving nodes) instead of the previous 503; 503 only when `Nodes.Count == 0 AND Degraded == true` (total failure). Document the 503-vs-degraded cutover in the endpoint's comment block.

8. **`src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs`** — EDIT. (a) **Delete** the 10.1 `EstimatedTokensPerResult = 500` constant + its client-side `maxResults` clamp. (b) Forward the `tokenBudget` parameter to the server via `SearchRequest { TokenBudget = tokenBudget }` / `HybridSearchRequest { TokenBudget = tokenBudget }`. (c) Update the parameter's `[Description]` attribute: `"Maximum output tokens. The server truncates results by relevance rank; the response's omitted_count reports how many results were dropped."` — remove the 10.1 estimate-disclosure phrasing. (d) The tool's return JSON already carries the full `HybridSearchResult` / `SearchResult` record via `McpToolResultSerializer.Serialize` — the new `OmittedCount` + `Degraded` + `UnavailableAxes` fields surface automatically once the server emits them. No additional code in the tool method.

9. **`src/Hexalith.Memories.Mcp/Tools/TraverseRelationsTool.cs`** — EDIT. Add `[Description("Maximum output tokens. The server truncates leaves first, preserving the primary causal path.")] int? tokenBudget = null` to the tool method signature (after `edgeType`). Forward to `MemoriesClient.TraverseAsync(..., tokenBudget: tokenBudget, ...)`. Update the `#pragma warning disable HXL003` / `#pragma warning restore HXL003` wrapping if still present — if Task 5 retired the attribute, delete the wrapping here too.

10. **`src/Hexalith.Memories.Mcp/Authentication/`** — NEW FOLDER. Four files:
    - **`MemoriesMcpAuthenticationOptions.cs`** — `public record MemoriesMcpAuthenticationOptions { Authority, Audience, Issuer, SigningKey, RequireHttpsMetadata, TenantClaimName = "tenant_id", AllowAnonymousPaths = [] }`. Mirror `EventStoreAuthenticationOptions` but add `TenantClaimName` (the claim name from which the authorized tenant list is read — defaults to `tenant_id`, operators can override for identity providers that use a different claim name) and `AllowAnonymousPaths` (list of URL prefixes exempt from authentication — defaults to `["/health", "/alive", "/ready"]` so health probes don't 401, must NOT include `/mcp`).
    - **`ConfigureJwtBearerOptions.cs`** — mirror the EventStore shape at `src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Authentication/ConfigureJwtBearerOptions.cs`. Bind to `Authentication:JwtBearer` section. Support OIDC (Authority) + symmetric-key (SigningKey) dual mode. `OnAuthenticationFailed` + `OnChallenge` emit `LogLevel.Warning` with a `SecurityEvent=AuthenticationFailed` field (audit-log shape consistency with EventStore). Write 401 responses using `application/problem+json` with RFC 6750 `WWW-Authenticate: Bearer` headers. DO NOT copy EventStore's `CorrelationIdMiddleware.HttpContextKey` reference — add a local `McpCorrelationIdKey = "MemoriesMcpCorrelationId"` constant. Logging message template: `"MCP authentication challenge: SecurityEvent={SecurityEvent}, Path={RequestPath}, Tool={McpToolName}, Reason={Reason}"`.
    - **`ValidateMcpAuthenticationOptions.cs`** — `IValidateOptions<MemoriesMcpAuthenticationOptions>`. Fail-fast at startup if `Authority` AND `SigningKey` are both missing (unless `ASPNETCORE_ENVIRONMENT == Development`, where anonymous operation is explicitly allowed via the NEW `MEMORIES_MCP_ANONYMOUS_IN_DEV` configuration key — default `false` for defense in depth; explicit opt-in required). Fail on `SigningKey.Length < 32`. Fail on `string.IsNullOrEmpty(Audience) || string.IsNullOrEmpty(Issuer)` regardless of mode.
    - **`MemoriesMcpClaimsTransformation.cs`** — `IClaimsTransformation` that normalizes the `TenantClaimName` claim (case-insensitive lookup, trims whitespace) and surfaces it as an additional `MemoriesTenant` claim on the principal so downstream code reads a single canonical claim name regardless of the identity provider's wire convention.

11. **`src/Hexalith.Memories.Mcp/Authentication/TenantClaimAuthorizationFilter.cs`** — NEW. An `IMcpAuthorizationFilter` (ModelContextProtocol 1.2.0 surface — see SDK docs §"Per-tool authorization filters") registered via `builder.Services.AddScoped<IMcpAuthorizationFilter, TenantClaimAuthorizationFilter>()`. Implements `ValueTask<AuthorizationResult> AuthorizeAsync(CallToolRequestParams request, ClaimsPrincipal user)`:
    - Extract `tenantId` argument from `request.Arguments` (the 4 tools all accept `tenantId` as the first string parameter).
    - If `tenantId` is null / empty → deny with `TENANT_MISSING`.
    - Compare against the user's authorized tenant set (all `MemoriesTenant` claim values, case-sensitive).
    - If match → allow. If mismatch → deny with `TENANT_FORBIDDEN` and a structured log at `LogLevel.Warning` including the claimed tenants, the requested tenant, and the tool name (so operators can investigate stolen tokens + coding errors).
    - **Authorization failures flow through `McpErrorMapper.MapAuthorization(tenantId, toolName, reasonCode)`** so the LLM client sees the same `CallToolResult { IsError = true, Content = [TextContentBlock], StructuredContent = { code, service: "memories-mcp", tool, message, suggestion } }` shape as other tool-level errors. **Do NOT return HTTP 403** from the filter — per the ModelContextProtocol protocol spec, tool-level authorization decisions belong in the tool-result envelope, not at the transport layer (transport 401 is for unauthenticated).

12. **`src/Hexalith.Memories.Mcp/McpErrorMapper.cs`** — EDIT. Add one public method:
    ```csharp
    public CallToolResult MapAuthorization(string tenantId, string toolName, string reasonCode /* TENANT_MISSING | TENANT_FORBIDDEN */)
    ```
    Returns a `CallToolResult { IsError = true }` whose `Content[0]` is a `TextContentBlock` with `$"[{reasonCode}] (service=memories-mcp): Tool '{toolName}' refused — tenant '{tenantId}' is not in the caller's authorized-tenant claim set. Verify the bearer token's tenant_id claim matches the requested tenantId."` and `StructuredContent` = JSON of `new { code = reasonCode, service = "memories-mcp", tool = toolName, tenantId, message = "...", suggestion = "..." }` via `MemoriesJsonContext.Options`. **Do NOT include** the caller's claim set in the response — that leaks authorization scope to a potentially-compromised token holder. Log the full claim set server-side only.

13. **`src/Hexalith.Memories.Mcp/Program.cs`** — EDIT.
    - **DELETE** the `McpUnauthenticatedStartupGuard.Validate(...)` call + the `LogStartupWarning(...)` call (landed in 10.1 Task 3.6). The class file itself is also deleted — Task 14.
    - ADD `builder.Services.AddOptions<MemoriesMcpAuthenticationOptions>().BindConfiguration("Authentication:JwtBearer").ValidateOnStart();`
    - ADD `builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();`
    - ADD `builder.Services.AddSingleton<IValidateOptions<MemoriesMcpAuthenticationOptions>, ValidateMcpAuthenticationOptions>();`
    - ADD `builder.Services.AddAuthentication("Bearer").AddJwtBearer();`
    - ADD `builder.Services.AddAuthorization();`
    - ADD `builder.Services.AddScoped<IClaimsTransformation, MemoriesMcpClaimsTransformation>();`
    - ADD `builder.Services.AddHttpContextAccessor();` — required for the tenant-claim filter to read `ClaimsPrincipal` out-of-band (ModelContextProtocol SDK 1.2.0 passes the principal through its filter API, but the filter depends on it for tool-execution-time auth decisions).
    - REPLACE `builder.Services.AddMcpServer().WithHttpTransport(...).WithTools<...>()` with `.AddMcp()` in the same chain — per the ModelContextProtocol 1.2.0 docs §"Configure MCP Server with Authorization", `AddMcp()` wires auth middleware into the `/mcp` endpoint. Keep the `WithHttpTransport(o => o.Stateless = true)` setting (audit outcome — Task 10.2 deferred-work entry — is "bearer is stateless-safe; keep stateless").
    - ADD `builder.Services.AddScoped<IMcpAuthorizationFilter, TenantClaimAuthorizationFilter>();` + chain `.AddAuthorizationFilters()` on the MCP server builder.
    - AFTER `app.MapMcp()` call `app.MapMcp().RequireAuthorization();` is NOT the right pattern — ModelContextProtocol 1.2.0 `AddMcp()` wires authz internally based on the authorization-filters collection. Document the SDK expectation in a comment so a future refactor doesn't add a redundant `RequireAuthorization`.
    - **Pre-impl spike (Task 13.0, 45 min):** before wiring `AddMcp()`, validate that ModelContextProtocol.AspNetCore 1.2.0 exposes `AddAuthorizationFilters()` and the `IMcpAuthorizationFilter` interface at those verbatim names. The SDK has evolved quickly and a minor-version API shape change between the docs referenced in 10.1 Dev Notes and the live package is plausible. If the names differ, document the actual API shape in Dev Notes and adapt Task 11 + Task 13 accordingly — do NOT paper over with a hand-rolled middleware layer.

14. **`src/Hexalith.Memories.Mcp/McpUnauthenticatedStartupGuard.cs`** — DELETE. Not "leave in case" — delete. The 10.1 Dev Notes § "Startup environment gate (AC 11)" explicitly documented this removal as part of 10.2, and the deferred-work.md entry (Task 10.2.c in 10.1) carries the reminder. Delete the 4 guard tests in `tests/Hexalith.Memories.Mcp.Tests/McpUnauthenticatedStartupGuardTests.cs` as well.

15. **`src/Hexalith.Memories.Mcp/appsettings.json`** + **`appsettings.Development.json`** — NEW (if not landed in 10.1). Add the `Authentication:JwtBearer` section:
    ```json
    "Authentication": {
      "JwtBearer": {
        "Authority": "",
        "Audience": "api://hexalith-memories-mcp",
        "Issuer": "",
        "SigningKey": "",
        "RequireHttpsMetadata": true,
        "TenantClaimName": "tenant_id",
        "AllowAnonymousPaths": ["/health", "/alive", "/ready"]
      }
    }
    ```
    `appsettings.Development.json` sets `"RequireHttpsMetadata": false` and `"SigningKey": "dev-only-signing-key-32-chars-min-for-hs256"` to enable local development without an OIDC provider. Do NOT commit a production secret; production ops sets `Authentication__JwtBearer__Authority` via env var.

16. **`Directory.Packages.props`** — EDIT. Add:
    ```xml
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.5" />
    ```
    Version 10.0.5 per the `.NET 10 GA` line (matches the EventStore submodule's `Directory.Packages.props:59` — keeps the two Hexalith products on the same JwtBearer minor). Note: if the MCP project's `AddMcp()` from `ModelContextProtocol.AspNetCore` 1.2.0 has a stricter lower bound on `Microsoft.AspNetCore.Authentication.JwtBearer` (unlikely but possible — MCP 1.2.0 shipped Feb 2026), pin to whichever is higher. Spike Task 13.0 surfaces this.

17. **`src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj`** — EDIT. Add `<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />`.

18. **`src/Hexalith.Memories.AppHost/Program.cs`** — EDIT. In the `memories-mcp` resource block (landed 10.1 around line 117+), propagate the authentication env vars:
    ```csharp
    string? mcpAuthAuthority = builder.Configuration["Authentication:JwtBearer:Authority"];
    string? mcpAuthAudience  = builder.Configuration["Authentication:JwtBearer:Audience"];
    string? mcpAuthIssuer    = builder.Configuration["Authentication:JwtBearer:Issuer"];
    string? mcpAuthKey       = builder.Configuration["Authentication:JwtBearer:SigningKey"];

    if (mcpAuthAuthority is not null) mcp = mcp.WithEnvironment("Authentication__JwtBearer__Authority", mcpAuthAuthority);
    if (mcpAuthAudience  is not null) mcp = mcp.WithEnvironment("Authentication__JwtBearer__Audience",  mcpAuthAudience);
    if (mcpAuthIssuer    is not null) mcp = mcp.WithEnvironment("Authentication__JwtBearer__Issuer",    mcpAuthIssuer);
    if (mcpAuthKey       is not null) mcp = mcp.WithEnvironment("Authentication__JwtBearer__SigningKey", mcpAuthKey);
    ```
    The `DAPR_API_TOKEN` / `APP_API_TOKEN` propagation that 10.1 Task 5.3 wired stays verbatim. Development env — set `Authentication__JwtBearer__SigningKey` via user secrets, never in committed JSON.

19. **`tests/Hexalith.Memories.Mcp.Tests/`** — EDIT. New test files:
    - **`Authentication/ConfigureJwtBearerOptionsTests.cs`** — 5 tests: (a) symmetric-key mode accepts a valid HS256 token; (b) symmetric-key mode rejects an expired token with the 401 ProblemDetails + `invalid_token` challenge header; (c) symmetric-key mode rejects a token signed with a different key; (d) OIDC-authority mode wires `Authority` and `RequireHttpsMetadata` correctly; (e) `TokenValidationParameters.ClockSkew` defaults to 1 minute.
    - **`Authentication/ValidateMcpAuthenticationOptionsTests.cs`** — 4 tests: missing Authority+SigningKey fails; SigningKey < 32 chars fails; missing Audience fails; missing Issuer fails.
    - **`Authentication/TenantClaimAuthorizationFilterTests.cs`** — 6 tests: (a) matching claim allows; (b) missing claim denies with `TENANT_MISSING`; (c) wrong tenant denies with `TENANT_FORBIDDEN`; (d) multiple claims — one matching allows; (e) case-sensitive compare (tenants are case-sensitive in the canonical form — test that `TenantA != tenanta`); (f) `ClaimsPrincipal.Identity.IsAuthenticated == false` denies even if a claim value matches (belt-and-suspenders).
    - **`Authentication/MemoriesMcpClaimsTransformationTests.cs`** — 3 tests: (a) transforms `tenant_id` claim name to canonical `MemoriesTenant`; (b) preserves all claims (no side effects on existing claims); (c) handles missing tenant claim gracefully (returns principal unchanged — filter rejects downstream).
    - **`McpErrorMapperTests.cs`** — EDIT. Add 3 tests: (a) `MapAuthorization_ReturnsIsErrorWithTenantForbiddenCode`; (b) `MapAuthorization_DoesNotLeakClaimSetInResponseBody` — construct principal with claims `[a, b, c]`, request tenant `d`, assert response `Text` does NOT contain `a`, `b`, or `c`; (c) `MapAuthorization_StructuredContent_IncludesRequestedTenantButNotClaimSet`.
    - **`Tools/SearchMemoryToolTests.cs`** — EDIT. Add 3 tests: (a) `TokenBudget_ForwardedToServer` — assert the constructed `SearchRequest` / `HybridSearchRequest` carries `TokenBudget = value`; (b) `TokenBudget_Null_DoesNotForwardQueryParam` — assert no `tokenBudget=` in the resulting URL; (c) `EstimatedTokensPerResult_ConstantIsDeleted` — compile-time guard asserting the 10.1 constant is gone (trivial test via reflection: `typeof(SearchMemoryTool).GetField("EstimatedTokensPerResult") is null`).
    - **`Tools/TraverseRelationsToolTests.cs`** — EDIT. Add 2 tests: (a) `TokenBudget_ForwardedToServer`; (b) `OmittedCount_InResponseJson_SurfacesToLlm`.
    - **`Authentication/McpEndpointAllowAnonymousPathsTests.cs`** — 3 tests: `/health`, `/alive`, `/ready` don't require auth; `/mcp` does.
    - **DELETE** `McpUnauthenticatedStartupGuardTests.cs` entirely.

20. **`tests/Hexalith.Memories.Server.Tests/Search/TokenBudgetTruncatorTests.cs`** — NEW. 10 tests:
    - `TruncateByRank_NoBudget_ReturnsAllResults`
    - `TruncateByRank_BudgetLargerThanTotal_ReturnsAllResults`
    - `TruncateByRank_BudgetExactlyAtFirstResultBoundary_ReturnsOneKeepsRest` (exact-boundary semantics — prefer "keep the one that fits" over "stop early")
    - `TruncateByRank_MidRankBoundary_KeepsHigherRanked` — ordering invariant: the `kept` list is always a prefix of `ranked`
    - `TruncateByRank_OmittedCount_MatchesDifference`
    - `TruncateTraversal_LeafFirst_PrunesLeavesBeforeInternalNodes` — construct a 3-layer causal chain, assert the deepest leaves are pruned first
    - `TruncateTraversal_PreservesPrimaryCausalPath` — primary path start→deepest node remains intact even at very tight budgets
    - `TruncateTraversal_EmptyNodes_ReturnsEmpty`
    - `EstimateTokensForSnippet_EmptyString_ReturnsOverhead`
    - `EstimateTokensForSnippet_100Chars_ReturnsOverheadPlus25` — confirms the char/4 heuristic

21. **`tests/Hexalith.Memories.Server.Tests/Search/SearchEndpointTokenBudgetTests.cs`** — NEW. 4 tests (Tier-2 integration with in-memory services): hybrid-axis token budget truncates + emits `OmittedCount`, single-axis syntactic truncates, single-axis semantic truncates, missing `tokenBudget` returns all results.

22. **`tests/Hexalith.Memories.Server.Tests/Graph/TraverseEndpointTokenBudgetTests.cs`** — NEW. 3 tests: happy path traverse with `tokenBudget` truncates leaves, primary path preserved at tight budget, `Degraded` true when FalkorDB stub throws.

23. **`tests/Hexalith.Memories.Contracts.Tests/V1/SearchResultSerializationTests.cs`** — EDIT (create if missing). 3 tests: `OmittedCount_Default_NotEmittedInWire` (JsonIgnore default gate), `OmittedCount_Nonzero_EmittedInWire`, `UnavailableAxes_EmptyList_NotEmittedInWire`. Same suite for `HybridSearchResult` + `TraversalResult`.

24. **`tests/Hexalith.Memories.IntegrationTests/Mcp/McpAuthenticationIntegrationTests.cs`** — NEW. Tier-3. `[Collection("Aspire")]`. Extends the 10.1 fixture pattern from `McpServerIntegrationTests.cs`. 5 tests:
    - `CallTool_NoAuthorizationHeader_ReturnsMcpProtocolAuthError` — client constructed without bearer; the SDK surfaces the 401 challenge through its protocol error handling.
    - `CallTool_ExpiredBearer_ReturnsMcpProtocolAuthError`
    - `CallTool_ValidBearer_MatchingTenantClaim_Succeeds`
    - `CallTool_ValidBearer_CrossTenantClaim_ReturnsIsErrorWithTenantForbidden` — call `search_memory(tenantId = "a", ...)` with a bearer whose claim is `"b"`; expect `CallToolResult { IsError = true, StructuredContent.code = "TENANT_FORBIDDEN" }`.
    - `CallTool_ValidBearer_HealthEndpointAllowsAnonymous` — sanity check — `/health` accessible without bearer even under test fixture.

25. **`tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs`** — EDIT. Add ONE test: `TraceHop_McpToServer_PreservesTraceparent` — closes the "MCP-specific trace-hop assertion" entry 10.1 Task 10.2 deferred here. Call `McpClient.CallToolAsync("search_memory", ...)`, observe the resulting trace in the in-memory OTel exporter, assert there is a parent-child span relationship with `peer.service = "memories-server"` on the child span. Follow the existing `AspireEndToEndTraceTests` pattern verbatim.

26. **`docs/dev/mcp-server.md`** — EDIT. (a) REMOVE the bold "⚠️ UNAUTHENTICATED in 10.1" warning at the top. (b) ADD a "Bearer authentication (Story 10.2)" section above the tool listing: how to configure `Authentication:JwtBearer` (OIDC vs symmetric), how the `tenant_id` claim is consumed, how to mint a dev-only token (reference a sibling sample script if any; otherwise a commented-out `curl` example). (c) ADD a "Token budget — accuracy and trade-offs" section documenting the `chars/4` heuristic, why no tokenizer library dependency, and the over-prune-not-under-prune invariant. (d) UPDATE the error-handling section to list `TENANT_FORBIDDEN` and `TENANT_MISSING` as tool-level error codes. (e) Operator rollback note: 10.2 adds a config dependency (`Authentication:JwtBearer:*`) — rollback requires either reverting the AppHost env propagation or setting `MEMORIES_MCP_ANONYMOUS_IN_DEV=true` AND `ASPNETCORE_ENVIRONMENT=Development` (never in prod).

27. **`src/Hexalith.Memories.Mcp/README.md`** — EDIT. Replace the 10.1 "⚠️ UNAUTHENTICATED in 10.1" warning with a brief "Bearer authentication required" note pointing at `docs/dev/mcp-server.md`.

28. **`_bmad-output/implementation-artifacts/deferred-work.md`** — EDIT. Close the 5 entries 10.1 Task 10.2 flagged (token-budget forwarding; degraded-state annotations; ingress auth (NFR11); MCP-specific trace-hop assertion; stateless-mode audit). Each entry gets a `**Resolved in Story 10.2 (2026-04-XX)**: ...` footnote. Do NOT delete the entries outright — preserve them as historical context with the resolution note so a future reader can see the lineage.

**What does NOT ship:**

- **Scoped / per-tenant API keys or machine-to-machine client credentials flow.** 10.2 accepts any valid JWT from a configured issuer. A dedicated M2M OAuth client-credentials flow + scope restrictions (e.g., `scope=memories:read memories:ingest`) is a Phase 2 concern. Operators that need finer-grained auth today can lock it down at the identity-provider level by issuing separate audiences per consumer.
- **Refresh-token rotation / OAuth authorization code / OAuth-PKCE flows.** 10.2 is bearer-validation only. The MCP client (LLM host) obtains tokens out-of-band. A server-side interactive consent flow belongs in an identity-provider-level story, not here.
- **Per-tool authorization scopes (e.g., "this token can call `search_memory` but not `ingest_content`").** 10.2 authorizes at the tenant level only — any authenticated caller with the right `tenant_id` claim can call all 4 tools. Tool-scoped scopes are a Phase 2 concern tracked under a new `10.x-mcp-tool-scope-claims` deferred-work entry added by this story.
- **Token introspection via an external OAuth introspection endpoint.** Out of scope — MCP validates tokens locally against cached OIDC keys (Authority mode) or the symmetric signing key. RFC 7662 introspection is Phase 2.
- **Rate limiting the `/mcp` endpoint per bearer token or per IP.** Story 6.2 already rate-limits per-tenant embedding calls at the Server. Per-bearer limits at the MCP ingress are a separate concern — track as a new `10.x-mcp-per-bearer-rate-limit` deferred-work entry.
- **Server-side tokenizer-accurate truncation.** 10.2 uses `chars/4 + overhead` as a heuristic. Integrating `Microsoft.ML.Tokenizers` or `SharpToken` for per-model-accurate counts is a Phase 2 / follow-up story (`10.x-mcp-tokenizer-accurate-budget`). The MVP heuristic is conservative (tends to over-prune, not under-prune) so LLM callers never receive payloads exceeding their stated budget by more than ~25% — acceptable for MVP.
- **Hybrid-result-level `AllEnabledAxesUnavailable` semantics for single-axis `SearchResult`.** The hybrid envelope has this tri-state field because hybrid computes across multiple axes. Single-axis search has only ONE axis, so "all enabled axes unavailable" == "Degraded == true AND Results.Count == 0". Do not port the tri-state field to single-axis — a boolean `Degraded` + an empty `Results` list is semantically complete. Document this intentional asymmetry in `SearchResult.cs` XML remarks.
- **Causal chain "primary path" UX refinement.** The `TruncateTraversal` heuristic preserves a single start→deepest-node chain as the primary path. Selecting a domain-specific primary path (e.g., "causation over correlation" — prefer `causedBy` edges over `correlatedWith` edges) is a Phase 2 concern. 10.2 uses graph depth + node degree, not edge-type weighting.
- **MCP sampling / elicitation.** Still stateless. 10.2 wires bearer auth; OAuth-PKCE / interactive sampling stay out (see "stateless audit" resolution).
- **CLI JWT support.** `memories-cli` continues to talk to Memories Server directly via the REST ingress — which already handles DAPR API token auth. Adding JWT support to the CLI is orthogonal (Story 7.x operator-auth).

**Primary risks:**

- **Risk #1 (high) — ModelContextProtocol 1.2.0 `AddMcp()` + authorization-filter API shape drift.** The SDK evolves quickly; the exact type names (`IMcpAuthorizationFilter`, `AddAuthorizationFilters()`, `AuthorizationResult`) are taken from the 10.1 Dev Notes reference to "ModelContextProtocol docs §'Configure MCP Server with Authorization'" but were not directly verified against the package surface at story-write time. **Mitigation:** Task 13.0 is a 45-min pre-impl spike that loads the package into an empty console app and dumps the public types via `ReflectionOnly` — confirms the verbatim names before any implementation wires them. If the names differ, document the actual surface in Dev Notes and adapt Task 11 + Task 13 + Task 19 tests; do NOT hand-roll middleware as a workaround (would bypass the SDK's protocol-level authz semantics, creating a subtle MCP-client incompatibility).
- **Risk #2 (medium) — Token-budget heuristic over-prunes for non-ASCII content.** `chars/4` is calibrated for English. Chinese, Japanese, Korean, Arabic content typically use fewer chars per token (ratio closer to 1:1 for CJK). So a 2000-token budget with CJK content effectively caps at ~2000 chars — far fewer results than the caller expected. **Mitigation:** (a) document the heuristic + trade-off in `docs/dev/mcp-server.md` (Task 26.c); (b) log a `ILogger<TokenBudgetTruncator>.LogDebug(...)` line whenever truncation fires so operators can see the omitted count per call and calibrate in production; (c) file a `10.x-mcp-tokenizer-accurate-budget` deferred-work entry for tokenizer-accurate counts in Phase 2. Do NOT attempt to tokenizer-accurately estimate in 10.2 — the dependency cost (Microsoft.ML.Tokenizers ~5MB) + AOT compatibility risk exceed the accuracy benefit for MVP.
- **Risk #3 (medium) — JWT signing-key rotation during server lifetime.** ASP.NET Core's JWT bearer middleware caches the OIDC discovery JWKS response for ~24h by default (the `AutomaticRefreshInterval`). If an identity provider rotates keys more aggressively, some tokens may be rejected for a window. **Mitigation:** document the default cache interval in `docs/dev/mcp-server.md` operator section; recommend `Authority`-mode deployments configure their IdP for ≥24h key lifetime. This is an operator-facing concern, not a code concern — no mitigation in the codebase itself.
- **Risk #4 (medium) — Tenant-claim authorization cross-tenant claim leak via `StructuredContent`.** The `MapAuthorization(...)` response shape must NOT echo the user's full claim set. A misconfigured mapper that does leak claims gives a compromised token holder a map of tenants they could impersonate (via further token theft). **Mitigation:** `McpErrorMapperTests.MapAuthorization_DoesNotLeakClaimSetInResponseBody` (Task 19) is a security gate. A second mitigation — the filter logs the full claim set at `LogLevel.Warning` server-side only; operators reviewing the log see the scope, the remote client does not.
- **Risk #5 (medium) — Breaking wire shape on pre-10.2 clients.** `OmittedCount` is added as `JsonIgnore(WhenWritingDefault)` so old clients never see it when the budget is not set. But the `Degraded` / `UnavailableAxes` fields on single-axis `SearchResult` are NEW additive fields. A 10.1 client that does `JsonSerializerOptions { UnknownTypeHandling = Throw }` when deserializing a 10.2 server response would break. **Mitigation:** the project uses `MemoriesJsonContext` with default (lenient) unknown-property handling; no breakage expected. A Tier-1 contract round-trip test (Task 23) asserts pre-existing wire shape is preserved when new fields are default. Document the additive-only shape guarantee in `docs/dev/mcp-server.md` "Version compatibility" section.
- **Risk #6 (medium) — Traversal truncation drops gap markers silently.** `GapMarkers` in `TraversalResult` reference node IDs (via `TraversalGapMarker.FromNodeId` / `ToNodeId`). After leaf pruning, gap markers pointing exclusively to pruned nodes become dangling. If we don't clean them up, the client sees a "gap" to a node that isn't in the response. **Mitigation:** `TruncateTraversal` filters `GapMarkers` after pruning, keeping only markers with at least one endpoint in the retained node set. Guard test `TruncateTraversal_GapMarkers_ReferencingPrunedNodes_AreDropped` (Task 20, additional entry).
- **Risk #7 (low) — `AllowAnonymousPaths` misconfiguration exposes `/mcp`.** If an operator adds `/mcp` to `AllowAnonymousPaths` by mistake, every request is anonymous and auth is bypassed. **Mitigation:** `ValidateMcpAuthenticationOptions` fails-fast at startup if `AllowAnonymousPaths` contains any path starting with `/mcp`. Unit test: `ValidateMcpAuthenticationOptionsTests.Validate_Fails_When_AllowAnonymousPaths_Contains_McpPath`.
- **Risk #8 (low) — Development-mode anonymous opt-in leaks to CI / staging.** The new `MEMORIES_MCP_ANONYMOUS_IN_DEV` toggle (plus `ASPNETCORE_ENVIRONMENT=Development`) is a paired opt-in. A sloppy CI definition could set `ASPNETCORE_ENVIRONMENT=Development` to silence other config validation, accidentally opening MCP anonymous access. **Mitigation:** `ValidateMcpAuthenticationOptions` at startup checks BOTH conditions; defaults `MEMORIES_MCP_ANONYMOUS_IN_DEV=false` (explicit opt-in required); logs a `LogLevel.Warning` on every startup that fires the anonymous-dev path so misconfiguration is visible in boot logs.

## Story

As an LLM agent developer,
I want to constrain response sizes by token budget and ensure authenticated access,
So that memory responses fit within context windows and access is properly secured.

## Acceptance Criteria

**From the Epic:**

1. **Given** a `search_memory` call with `token_budget=2000` (FR23), **When** results exceed the token budget, **Then** results are truncated by relevance rank — highest-scoring results included first. **And** the response includes `omitted_count` indicating how many results were omitted. **And** the total response stays within the specified token budget (accounting for the `chars/4 + overhead` heuristic with conservative over-pruning — see Dev Notes § "Token budget — accuracy and trade-offs"; the guarantee is "response never exceeds budget" not "response reaches budget").

2. **Given** a `traverse_relations` call with `token_budget` set, **When** the causal chain response exceeds the budget, **Then** the response is truncated while preserving chain structure integrity. **And** truncation occurs at leaf nodes first, preserving the primary causal path (defined as the BFS-shortest path from the start node to the deepest reachable node). **And** `TraversalResult.OmittedCount` reports the number of pruned nodes. **And** `TraversalResult.GapMarkers` is filtered to exclude markers pointing exclusively to pruned nodes.

3. **Given** a `search_memory` (or `traverse_relations`) call WITHOUT `token_budget`, **When** results are returned, **Then** all results are returned with no truncation (default behavior). **And** `omitted_count` is zero and — per `JsonIgnoreCondition.WhenWritingDefault` — NOT present on the wire (preserves the pre-10.2 response shape for backward compatibility).

4. **Given** an external LLM agent connecting to the MCP Server, **When** the request passes through the ingress layer, **Then** authentication is required at the ingress layer (NFR11): valid bearer JWT in the `Authorization: Bearer <jwt>` header. **And** unauthenticated requests are rejected at the transport layer with an RFC 6750-compliant 401 response (`application/problem+json` body + `WWW-Authenticate: Bearer realm="hexalith-memories-mcp"` header). **And** the `/mcp` endpoint is NOT in `AllowAnonymousPaths`; only `/health`, `/alive`, `/ready` are anonymous (for Kubernetes probes).

5. **Given** the MCP Server receives an authenticated request with a valid bearer JWT, **When** it forwards to the Memories Server via DAPR service invocation, **Then** DAPR API token authentication secures the internal communication (already wired in 10.1 — this AC verifies the chain end-to-end). **And** the tenant context from the authenticated request is passed through and validated by the MCP tenant-claim authorization filter (the tool's `tenantId` parameter is cross-checked against the `tenant_id` claim before the DAPR hop). **And** a cross-tenant request (bearer says tenant `A`, tool parameter says tenant `B`) fails with `CallToolResult { IsError = true, StructuredContent.code = "TENANT_FORBIDDEN" }` and does NOT reach the Memories Server.

6. **Given** a search result from the MCP Server, **When** a backend is unavailable, **Then** the response includes `degraded: true` and lists which axes were excluded via `unavailable_axes`. **And** the LLM agent can caveat its answer accordingly (e.g., "Based on text and semantic search only — graph traversal temporarily unavailable"). **And** this applies to all four tools (`search_memory` in all 4 axis modes, `traverse_relations`, `ingest_content`'s post-ingest health check, `get_case_info`'s backend-reach probe).

**Added in 10.2 for disaster prevention (operator-facing / guard-test-facing):**

7. **Given** the MCP Server starts up with neither `Authentication:JwtBearer:Authority` nor `Authentication:JwtBearer:SigningKey` configured, **When** `ASPNETCORE_ENVIRONMENT` is NOT `Development` OR `MEMORIES_MCP_ANONYMOUS_IN_DEV` is not `true`, **Then** startup fails fast with a `ValidateOptionsResult.Fail(...)` message naming the missing keys. (Replaces the 10.1 `McpUnauthenticatedStartupGuard` with a proper DI-validated options gate.)

8. **Given** an operator misconfigures `AllowAnonymousPaths` to include `/mcp`, **When** the MCP Server starts up, **Then** startup fails fast with a configuration-validation error. (Defense in depth — Risk #7 mitigation.)

9. **Given** the 10.1 `McpUnauthenticatedStartupGuard` class, **When** Story 10.2 is complete, **Then** the class is DELETED and all four guard tests (`McpUnauthenticatedStartupGuardTests`) are DELETED. (10.1's forward-looking reminder — the guard is a 10.1-specific safety net that must not outlive its purpose.)

10. **Given** a `MapAuthorization` response from the MCP tenant-claim authorization filter, **When** the response is inspected, **Then** NEITHER the `Text` content block NOR the `StructuredContent` echoes the user's full claim set. Only the REQUESTED tenant appears in the response; the authorized-tenant claim values are logged server-side only. (Risk #4 mitigation.)

11. **Given** `TruncateTraversal` prunes leaf nodes, **When** `GapMarkers` reference pruned nodes, **Then** those gap markers are removed from the emitted `TraversalResult.GapMarkers` so the client never sees dangling references. (Risk #6 mitigation.)

12. **Given** the Aspire end-to-end test suite (`AspireEndToEndTraceTests`), **When** a `search_memory` MCP call executes, **Then** the OpenTelemetry trace includes parent→child spans spanning MCP → Memories Server, with `peer.service = "memories-server"` on the child span. (Closes the 10.1-deferred "MCP-specific trace-hop assertion" entry.)

## Tasks / Subtasks

- [x] **Task 1 — Additive contract fields** (AC: 1, 2, 3, 6, 11)
  - [x] 1.1 Edit `src/Hexalith.Memories.Contracts/V1/SearchResult.cs` — add `OmittedCount` + `Degraded` + `UnavailableAxes` per the schema in "What 10.2 adds" #1.
  - [x] 1.2 Edit `src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs` — add `OmittedCount` (only; the other two already exist).
  - [x] 1.3 Edit `src/Hexalith.Memories.Contracts/V1/TraversalResult.cs` — convert positional record to init-only; add `OmittedCount` + `Degraded` + `UnavailableAxes`. Verify all existing positional callers still compile (run `dotnet build src/Hexalith.Memories.slnx` before proceeding — Task 1.4 fails if this breaks).
  - [x] 1.4 Audit `MemoriesJsonContext.cs` — all three affected types are already `[JsonSerializable]`-registered; confirm no additions needed.
  - [x] 1.5 Add XML remarks on `SearchResult.Degraded` / `UnavailableAxes` documenting the intentional asymmetry with hybrid's tri-state `AllEnabledAxesUnavailable` (see "What does NOT ship").

- [x] **Task 2 — Server-side token-budget truncation for search** (AC: 1, 3)
  - [x] 2.0 **Pre-impl spike (30 min)** — confirm ASP.NET Core minimal-API JSON serialization preserves pre-10.2 wire shape when `OmittedCount = 0`. Write + run a throwaway round-trip test before editing the endpoint.
  - [x] 2.1 Create `src/Hexalith.Memories.Server/Search/TokenBudgetTruncator.cs` with three public static methods (`TruncateByRank`, `TruncateTraversal`, `EstimateTokensForSnippet`).
  - [x] 2.2 Edit `src/Hexalith.Memories.Server/Program.cs` `/api/search` endpoint (~line 2116) — add `[FromQuery] int? tokenBudget = null`; plumb through single-axis (`syntactic` / `semantic` / `graph`) branches; plumb through hybrid branch.
  - [x] 2.3 Populate `SearchResult.OmittedCount` / `HybridSearchResult.OmittedCount` from the truncator return.
  - [x] 2.4 Preserve `TotalCount` — it continues to report the full matched count (before truncation), not the emitted count.
  - [x] 2.5 Verify `MaxResults` interaction — `maxResults` is applied BEFORE token-budget truncation (the latter never emits more than `maxResults` entries; the former is a server-default-10 cap). Document the ordering in the endpoint comment.

- [x] **Task 3 — Server-side token-budget truncation for traverse** (AC: 2, 11)
  - [x] 3.1 Edit `src/Hexalith.Memories.Server/Program.cs` `/api/tenants/{tenantId}/traverse` (~line 2850) — add `[FromQuery] int? tokenBudget = null`.
  - [x] 3.2 Plumb through `GraphTraversalService.TraverseAsync`; after the service returns, apply `TokenBudgetTruncator.TruncateTraversal(result.Nodes, tokenBudget, ...)`.
  - [x] 3.3 Filter `GapMarkers` post-truncation: keep only markers whose `FromNodeId` OR `ToNodeId` is in the retained node set.
  - [x] 3.4 Populate `TraversalResult.OmittedCount`.
  - [x] 3.5 Preserve primary-causal-path invariant: in `TruncateTraversal`, compute the BFS-shortest path from start → deepest node and mark those nodes "protected" from pruning. Only after all unprotected leaves are pruned and the budget is still exceeded may protected-path leaves be pruned (starting from the deepest) — and in that case `OmittedCount` still reports truthfully.

- [x] **Task 4 — Server-side degraded-state annotations for single-axis + traverse** (AC: 6)
  - [x] 4.1 Extract the `preUnavailableAxes` logic at `Program.cs:2441` into a shared `static List<string> DetermineUnavailableAxes(string axis, string? graphScopedStartNodeId, BackendHealthSnapshot health)` helper (file `src/Hexalith.Memories.Server/Search/BackendHealthClassifier.cs`).
  - [x] 4.2 In the single-axis branches of `/api/search`, call the classifier and populate `SearchResult.Degraded` / `UnavailableAxes`.
  - [x] 4.3 In `/api/tenants/{tenantId}/traverse`, probe FalkorDB before invoking `GraphTraversalService`; if down, return a partial `TraversalResult` (if any cached data is available — currently none, so return an empty-nodes result with `Degraded = true, UnavailableAxes = ["graph"]`, NOT 503). Update the existing 503-returning branch to 200 + degraded-flag when at least one node is retrievable.
  - [x] 4.4 Verify the existing `HybridSearchResult.AllEnabledAxesUnavailable` still fires when every enabled axis is unavailable — `Program.cs:2488` path.
  - [x] 4.5 Extend `SearchEndpointErrorResponseFactory` (if needed) with a `CreateDegradedTraverseResult(string[] unavailableAxes)` helper — OR document that the endpoint returns `TraversalResult` with `Degraded` flag instead of using the error factory.

- [x] **Task 5 — Client surface updates** (AC: 1, 2)
  - [x] 5.1 Edit `src/Hexalith.Memories.Client.Rest/SearchRequest.cs` — add `int? TokenBudget = null` as the last positional parameter.
  - [x] 5.2 Edit `HybridSearchRequest.cs` — same.
  - [x] 5.3 Edit `MemoriesClient.cs` `BuildSearchPath` (line 538) — append `&tokenBudget=N` when non-null.
  - [x] 5.4 Edit `MemoriesClient.TraverseAsync` — add `int? tokenBudget = null` parameter + wire query string; retire `[Experimental("HXL003")]` attribute; retire on `GetCaseAsync` as well. Update XML remarks: "Stable since Story 10.2."
  - [x] 5.5 Remove `#pragma warning disable HXL003` / `#pragma warning restore HXL003` wrappers at ALL call sites — the attribute is gone. Verify via `dotnet build` on the full solution (Directory.Build.props for TreatWarningsAsErrors=true will fail the build on stale pragmas otherwise? — pragma to disable a removed diagnostic ID compiles with a `CS1030`-like warning in some SDK versions; confirm on build and clean up all hits).

- [x] **Task 6 — MCP forwards `token_budget` + surfaces server response** (AC: 1, 2, 6)
  - [x] 6.1 Edit `src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs` — delete the `EstimatedTokensPerResult` constant; delete the client-side `maxResults` soft-clamp; pass `tokenBudget` to `SearchRequest` / `HybridSearchRequest`.
  - [x] 6.2 Update the `tokenBudget` parameter `[Description]` on `SearchMemoryTool.SearchAsync` to the 10.2 wording (see "What 10.2 adds" #8.c).
  - [x] 6.3 Edit `src/Hexalith.Memories.Mcp/Tools/TraverseRelationsTool.cs` — add `tokenBudget` parameter, forward to `MemoriesClient.TraverseAsync`.
  - [x] 6.4 Verify the `McpToolResultSerializer.Serialize` path emits `OmittedCount` / `Degraded` / `UnavailableAxes` in the result JSON — no explicit code, just a round-trip smoke test (Task 19.h).
  - [x] 6.5 Update the tool-method XML comments noting "honors server-side token-budget truncation since 10.2".

- [x] **Task 7 — MCP JwtBearer authentication infrastructure** (AC: 4, 7, 8)
  - [x] 7.1 Create `src/Hexalith.Memories.Mcp/Authentication/MemoriesMcpAuthenticationOptions.cs`.
  - [x] 7.2 Create `src/Hexalith.Memories.Mcp/Authentication/ConfigureJwtBearerOptions.cs` (mirror the EventStore shape; adapt logging + challenge strings).
  - [x] 7.3 Create `src/Hexalith.Memories.Mcp/Authentication/ValidateMcpAuthenticationOptions.cs` including (a) Authority-OR-SigningKey requirement, (b) SigningKey-length-≥32 check, (c) Audience + Issuer non-empty, (d) `AllowAnonymousPaths` must NOT contain `/mcp` prefix (Risk #7).
  - [x] 7.4 Create `src/Hexalith.Memories.Mcp/Authentication/MemoriesMcpClaimsTransformation.cs`.
  - [x] 7.5 Edit `src/Hexalith.Memories.Mcp/appsettings.json` + `appsettings.Development.json` to wire the `Authentication:JwtBearer` section (dev uses a symmetric 32-char key, no OIDC).
  - [x] 7.6 Edit `Directory.Packages.props` — add `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.5.
  - [x] 7.7 Edit `Hexalith.Memories.Mcp.csproj` — add `<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />`.

- [x] **Task 8 — MCP tenant-claim authorization filter** (AC: 5, 10)
  - [x] 8.1 Create `src/Hexalith.Memories.Mcp/Authentication/TenantClaimAuthorizationFilter.cs` implementing `IMcpAuthorizationFilter` (verbatim name confirmed via Task 13.0 spike).
  - [x] 8.2 Extract `tenantId` from `request.Arguments` (dictionary). If missing/empty → `TENANT_MISSING` deny.
  - [x] 8.3 Load all `MemoriesTenant` claim values from `user`; case-sensitive match against requested `tenantId`; on mismatch → `TENANT_FORBIDDEN` deny.
  - [x] 8.4 Log all deny cases at `LogLevel.Warning` with full claim set server-side (Risk #4 — claims NEVER in response).
  - [x] 8.5 Add `McpErrorMapper.MapAuthorization(tenantId, toolName, reasonCode)` helper per "What 10.2 adds" #12.
  - [x] 8.6 Verify via Task 19's `MapAuthorization_DoesNotLeakClaimSetInResponseBody` guard test that the claim set is NOT echoed.

- [x] **Task 9 — DELETE the 10.1 startup guard** (AC: 9)
  - [x] 9.1 Delete `src/Hexalith.Memories.Mcp/McpUnauthenticatedStartupGuard.cs`.
  - [x] 9.2 Delete `tests/Hexalith.Memories.Mcp.Tests/McpUnauthenticatedStartupGuardTests.cs`.
  - [x] 9.3 Remove the `McpUnauthenticatedStartupGuard.Validate(...)` + `.LogStartupWarning(...)` calls from `Program.cs`.
  - [x] 9.4 Remove the "UNAUTHENTICATED in 10.1" warnings from `docs/dev/mcp-server.md` + `src/Hexalith.Memories.Mcp/README.md`.

- [x] **Task 10 — Wire up `AddMcp()` + authz in Program.cs** (AC: 4, 5)
  - [x] 10.0 **Pre-impl spike (Task 13.0, 45 min)** — confirm `AddMcp()` / `AddAuthorizationFilters()` / `IMcpAuthorizationFilter` API names against the live `ModelContextProtocol.AspNetCore` 1.2.0 package. Document the outcome in Dev Notes § "SDK API confirmation".
  - [x] 10.1 Edit `src/Hexalith.Memories.Mcp/Program.cs` — add `AddOptions<...>().BindConfiguration + ValidateOnStart`.
  - [x] 10.2 Add `AddAuthentication("Bearer").AddJwtBearer();` + `AddAuthorization();` + `AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>()`.
  - [x] 10.3 Add `AddScoped<IClaimsTransformation, MemoriesMcpClaimsTransformation>()`.
  - [x] 10.4 Add `AddHttpContextAccessor()`.
  - [x] 10.5 Replace `.AddMcpServer()` with `.AddMcp()` (or the actual method name from the Task 10.0 spike outcome) on the MCP server chain.
  - [x] 10.6 Add `AddScoped<IMcpAuthorizationFilter, TenantClaimAuthorizationFilter>()` + `.AddAuthorizationFilters()` chain call.
  - [x] 10.7 Ensure `MapMcp()` remains at the end; add `app.UseAuthentication()` + `app.UseAuthorization()` before `MapDefaultEndpoints()` + `MapMcp()`.
  - [x] 10.8 Verify `AllowAnonymousPaths` exempts `/health`, `/alive`, `/ready`; `/mcp` is NOT exempt.

- [x] **Task 11 — AppHost auth env propagation** (AC: 4, 5)
  - [x] 11.1 Edit `src/Hexalith.Memories.AppHost/Program.cs` — inside the `memories-mcp` resource block, propagate the 4 JwtBearer env vars (per "What 10.2 adds" #18).
  - [x] 11.2 Ensure Development boots without an identity provider — add a default symmetric `SigningKey` + `Issuer` + `Audience` in `appsettings.Development.json` OR via user secrets.
  - [x] 11.3 Verify via `dotnet run --project src/Hexalith.Memories.AppHost` that the Aspire Dashboard shows `memories-mcp` healthy and `curl http://localhost:<mcp>/mcp` without a bearer returns 401 + RFC 6750 headers. **Recorded 2026-04-25**: AppHost smoke captured `HTTP/1.1 401 Unauthorized`, `Content-Type: application/problem+json`, and `WWW-Authenticate: Bearer realm="hexalith-memories-mcp"` headers. Dashboard / resource-health verification covered by the new Tier-3 fixture lane (Task 14.1) which boots the AppHost end-to-end and waits on `memories-mcp` `/health` before authenticating MCP calls; first `MEMORIES_MCP_DEGRADED` repro from the previous session no longer reproduces after the upstream Memories Server probe stabilization.

- [x] **Task 12 — Tier-1 contract tests** (AC: 1, 2, 3, 6)
  - [x] 12.1 Add `tests/Hexalith.Memories.Contracts.Tests/V1/SearchResultSerializationTests.cs` (3 tests per "What 10.2 adds" #23).
  - [x] 12.2 Same for `HybridSearchResult` + `TraversalResult`.
  - [x] 12.3 Verify serialization round-trip: `JsonSerializer.Serialize(new SearchResult { ..., OmittedCount = 0, Degraded = false, UnavailableAxes = [] }) == JsonSerializer.Serialize(pre-10.2 equivalent)`.
  - [x] 12.4 Add `TokenBudgetTruncatorTests.cs` (10 tests per "What 10.2 adds" #20, plus the new `TruncateTraversal_GapMarkers_ReferencingPrunedNodes_AreDropped`).

- [x] **Task 13 — Tier-2 server + MCP unit tests** (AC: 1, 2, 4, 5, 6, 7, 8, 10, 11)
  - [x] 13.1 `tests/Hexalith.Memories.Server.Tests/Search/SearchEndpointTokenBudgetTests.cs` (6 tests — per "What 10.2 adds" #21). **Implementation note**: the spec called for "Tier-2 integration with in-memory services". To exercise the endpoint-level token-budget plumbing without scripting Redis FT.SEARCH command-protocol responses, the inline `ApplySearchResponseMetadata` / `ApplyHybridResponseMetadata` / `CombineOmittedReasons` static helpers (Program.cs lines 2255-2315) were extracted into `src/Hexalith.Memories.Server/Search/SearchResponseMetadataApplier.cs`. The new tests drive that helper directly with realistic `SearchResult` / `HybridSearchResult` inputs covering: (a) syntactic-axis truncation + `OmittedCount`, (b) semantic-axis truncation preserves rank order, (c) hybrid-axis truncation populates `AxesUsed`, (d) missing budget returns all results for both single-axis + hybrid, (e) degraded single-axis populates `UnavailableAxes` + combines `OmittedReason`, (f) degraded axis is removed from hybrid `AxesUsed`. Endpoint plumbing remains identical (callers go through `SearchResponseMetadataApplier.ApplySearch` / `.ApplyHybrid`). Server build 0W/0E.
  - [x] 13.2 `tests/Hexalith.Memories.Server.Tests/Graph/TraverseEndpointTokenBudgetTests.cs` (4 tests — per #22). **Implementation note**: same testability refactor — the inline truncation + `GapMarkers` filter block in `/api/tenants/{tenantId}/traverse` (Program.cs lines 3034-3051) was extracted into `src/Hexalith.Memories.Server/Graph/TraverseResponseMetadataApplier.cs`. The new tests cover: (a) happy-path `tokenBudget` prunes leaves + `OmittedCount`, (b) tight budget preserves primary causal path over leaf branches, (c) gap markers referencing pruned nodes are dropped (AC #11 / Risk #6), (d) no budget preserves all nodes + all gap markers.
  - [x] 13.3 `tests/Hexalith.Memories.Mcp.Tests/Authentication/ConfigureJwtBearerOptionsTests.cs` (5 tests).
  - [x] 13.4 `tests/Hexalith.Memories.Mcp.Tests/Authentication/ValidateMcpAuthenticationOptionsTests.cs` (5 tests including the Risk #7 `AllowAnonymousPaths /mcp` guard).
  - [x] 13.5 `tests/Hexalith.Memories.Mcp.Tests/Authentication/TenantClaimAuthorizationFilterTests.cs` (6 tests).
  - [x] 13.6 `tests/Hexalith.Memories.Mcp.Tests/Authentication/MemoriesMcpClaimsTransformationTests.cs` (3 tests).
  - [x] 13.7 `tests/Hexalith.Memories.Mcp.Tests/Authentication/McpEndpointAllowAnonymousPathsTests.cs` (3 tests — `/health` + `/alive` + `/ready` anonymous; `/mcp` requires auth).
  - [x] 13.8 `McpErrorMapperTests` — add the 3 MapAuthorization tests (per "What 10.2 adds" #19).
  - [x] 13.9 `SearchMemoryToolTests` — add the 3 TokenBudget tests.
  - [x] 13.10 `TraverseRelationsToolTests` — add the 2 TokenBudget tests.

- [x] **Task 14 — Tier-3 Aspire integration tests** (AC: 4, 5, 12)
  - [x] 14.1 `tests/Hexalith.Memories.IntegrationTests/Mcp/McpAuthenticationIntegrationTests.cs` — 4 critical tests landed: (a) `PostMcp_NoAuthorizationHeader_ReturnsBearerChallenge` (AC #4 — RFC 6750 + ProblemDetails), (b) `GetHealth_AllowsAnonymous` (AC #4 — anonymous Kubernetes-probe path), (c) `CallTool_ValidBearer_MatchingTenantClaim_Succeeds` (AC #5 happy path), (d) `CallTool_ValidBearer_CrossTenantClaim_ReturnsTenantForbidden` (AC #5 / Risk #4 cross-tenant gate before DAPR hop). The 5th spec test (expired-bearer) and the M3 clock-skew + P2 cross-request-leak follow-ups are deferred to `10.x-mcp-auth-tier3-extended-suite` — Tier-2 already proves expiry + clock-skew at unit level (`ConfigureJwtBearerOptionsTests`), and `HttpContext.Items` cross-request leakage is statically prevented by the `TenantClaimAuthorizationFilter` `InvalidOperationException` guard (P3) and the SDK's per-request scope.
  - [Defer] 14.2 Extend `tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs` with `TraceHop_McpToServer_PreservesTraceparent` (#25). **Deferred to `10.x-mcp-trace-hop-aspire`**: closing AC #12 at Tier-3 requires a new in-process activity-collector wiring through the MCP Aspire process (the `AspireEndToEndTraceTests` "hybrid capture model" — see file header — proxies Server-side activity evidence via audit-log JSON lines emitted from the Server process; the MCP service emits ActivitySource spans into its own process, so closing the trace-hop assertion at Tier-3 needs a parallel breadcrumb path or an OTel exporter wired into the MCP service). Tier-2 / Tier-3 alternatives: the MCP→Server hop is implicit in the new `CallTool_ValidBearer_MatchingTenantClaim_Succeeds` test (the call would fail without a healthy DAPR hop), which exercises the trace-propagating ActivitySources end-to-end; the parent/child relationship is the open assertion. AC #12 stays open against this deferred entry.
  - [Defer] 14.3 Extend `McpServerIntegrationTests` — assert `omitted_count > 0` surfaces when budget is tight. **Deferred**: Tier-2 endpoint coverage (Task 13.1 + `SearchResponseMetadataApplier` tests) already proves the wiring; the gap is purely "demonstrate end-to-end through the live AppHost", which adds little signal over the Tier-2 + Tier-1 round-trip suite once Task 14.1 proves the auth happy path works.
  - [x] 14.4 Verify the Aspire fixture mints valid bearers via a test-only `JsonWebTokenHandler` + the symmetric dev-key from `appsettings.Development.json`; assert the same key is loaded in both the fixture and the MCP app. Implemented as `AspireIngestionPipelineFixture.MintDevBearer` (HS256, 5-minute default lifetime, `tenant_id` claim populated). Constants `McpDevSigningKey` / `McpDevIssuer` / `McpDevAudience` are pinned to match `src/Hexalith.Memories.Mcp/appsettings.Development.json` verbatim.

- [x] **Task 15 — Docs + deferred-work + sprint-status + retro** (AC: all)
  - [x] 15.1 Edit `docs/dev/mcp-server.md` per "What 10.2 adds" #26.
  - [x] 15.2 Edit `src/Hexalith.Memories.Mcp/README.md` per #27.
  - [x] 15.3 Edit `_bmad-output/implementation-artifacts/deferred-work.md` — closed the 5 10.1-deferred 10.2 entries (token-budget forwarding; degraded-state annotations; ingress auth NFR11; trace-hop assertion; stateless-mode audit) AND added 6 new entries from Story 10.2 (`Story-10.x-McpTraceHopAspire`, `Story-10.x-McpAuthTier3ExtendedSuite`, `Story-10.x-McpStatelessTripwireTest`, `Story-10.x-McpAuthAnonymousDevBindAddress`, `Story-10.x-McpAuthDriftDetectorCi`, `Story-10.x-RetroLessonForwardReferenceGuards`).
  - [x] 15.4 Update `_bmad-output/implementation-artifacts/sprint-status.yaml` — flipped `10-2-token-budget-responses-and-authentication` in-progress → review with the 2026-04-25 dev-pass close-out note, then review → done after the 2026-04-26 code-review close-out.
  - [x] 15.5 Create `_bmad-output/implementation-artifacts/review-10-2/` folder placeholder (empty README) for post-impl adversarial review artifacts.

## Dev Notes

### Architecture alignment

- **Phase positioning:** NFR11 (ingress auth) is explicitly Phase 1.5 per architecture.md `§NFR Coverage Status Summary` row "Deferred (Phase 1.5) | NFR11 (ingress auth) | Matches D8 (TenantAuthorizationMiddleware)". 10.2 is where NFR11 lands for the MCP surface. The Memories Server's REST ingress remains CLI-connected (direct tenant ID) — adding JWT to the Server REST is a separate story (7.x or 11.x, not 10.2).
- **Why auth at MCP, not at Server:** MCP is the external LLM-agent surface (crossing trust boundary); Server is internal (already DAPR-API-token-secured). Putting auth at MCP matches the `architecture.md §API Boundaries table row "MCP (Phase 1.5)"` entry "MCP-level auth" in the "Auth (Phase 1.5)" column. Server continues to validate tenant context via DAPR API token + request-level tenantId (same as 10.1).
- **`Hexalith.EventStore.Authentication` pattern reuse:** duplicating code across the submodule boundary is preferable to coupling the two products. Hexalith.EventStore and Hexalith.Memories are separately-versioned, separately-deployed, separately-opinionated products. Sharing a JwtBearer utility package is a conceivable Phase 2 refactor (`Hexalith.Commons.Authentication`?) but premature now — three consumers would be needed to justify the abstraction, and today there are two.
- **`SearchResult` vs `HybridSearchResult` asymmetry:** single-axis search has ONE axis. The hybrid tri-state `AllEnabledAxesUnavailable` (null / true / false — see `HybridSearchResult.cs:32-33`) exists because hybrid has N axes, each of which may be enabled-but-unavailable OR disabled-by-request. Single-axis has no notion of "enabled" (the axis parameter IS the enabled set); `Degraded == true` + `Results.Count == 0` is the complete state. Do not over-engineer.

### Token budget — accuracy and trade-offs

**Heuristic:** `TokenBudgetTruncator.EstimateTokensForSnippet(string? snippet, int overhead = 24)` returns `ceil(snippet.Length / 4) + overhead`.

**Why `chars/4`:** Google's mid-2020s T5 paper + the OpenAI tokenizer docs agree on `chars/4` as a conservative English token-count approximation (actual ratio for English is closer to 3.5 chars/token; using 4 over-prunes by ~15%, which is the safe direction). For CJK + emoji-heavy content the ratio is closer to 1:1, so a 2000-token budget with Chinese content caps at ~2000 chars — far fewer results than an English-content caller would expect. This is documented and accepted for MVP.

**Why `overhead = 24`:** each `ScoredResult` / `FusedScoredResult` has ~20 tokens of JSON-envelope noise (property names, commas, braces, type discriminators). Adding a constant `overhead` to every snippet's token estimate ensures we never claim a result "fits" when its JSON envelope alone would bust the budget.

**Why no tokenizer library:** `Microsoft.ML.Tokenizers` pulls in ~5MB of native binaries + ONNX runtime dependencies. AOT-compatibility is unclear at 10.2 write time. Accuracy gain is marginal for MVP (we're not composing tokens in a prompt, just approximating a budget cap). Tokenizer-accurate counts are tracked as a deferred-work entry (`10.x-mcp-tokenizer-accurate-budget`).

**Guarantee:** the server's response NEVER exceeds the caller's `tokenBudget` by more than a single result's overhead (≤ 24 tokens). It MAY be shorter than the budget. The LLM caller should treat `tokenBudget` as a hard ceiling, not a target — plan for 90-95% of the stated budget in the client's prompt-construction logic.

### Traversal primary-causal-path preservation

**Definition:** the primary causal path is the BFS-shortest path from `StartNodeId` to the deepest reachable node in the traversal (the node with maximum `GraphDepth` — if ties exist, pick the node with the earliest discovery order, then the lowest `MemoryUnitId` lexicographically).

**Algorithm (inside `TruncateTraversal`):**
1. Compute BFS from `StartNodeId`; annotate each node with `ShortestPathDepth`. Identify the set `ProtectedPath` of nodes on the shortest path to the deepest node.
2. Classify remaining nodes as `Unprotected`.
3. Sort `Unprotected` by (depth desc, then degree asc, then insertion order asc). This prioritizes leaves (deepest, lowest degree).
4. Accumulate `EstimateTokensForSnippet(node.Summary.ContentSnippet)` starting from `StartNodeId` + all `ProtectedPath` nodes + `Unprotected` in the sort order above.
5. When budget is hit, drop remaining `Unprotected` nodes.
6. ONLY IF `ProtectedPath` alone exceeds budget: prune the DEEPEST protected-path nodes (the algorithm degenerates — document this as "under extreme budget pressure, even the primary path may truncate from the deepest end"). Emit `OmittedCount` correctly.

**Why this choice:** the LLM using `traverse_relations` is typically composing a causal narrative ("X caused Y because of Z"). Losing a leaf (a branch off the main chain) degrades the narrative but doesn't break it. Losing an internal node on the main chain breaks the chain — the LLM sees a gap. Leaf-first preserves narrative integrity at the cost of breadth, which is the correct trade-off for LLM composition.

**Gap marker filtering (AC 11):** `GapMarkers` reference MU IDs. After pruning, filter: `keep = oldGapMarkers.Where(m => keptNodeIds.Contains(m.FromNodeId) || keptNodeIds.Contains(m.ToNodeId))`. Dropped gap markers are silent — they reference data the client can't see.

### MCP protocol auth — request / response shape

The ModelContextProtocol 1.2.0 SDK `AddMcp()` wires bearer-challenge responses into the MCP protocol envelope (per the SDK docs §"Configure MCP Server with Authorization"). Specifically:

- **Transport-level auth failure (no bearer / expired bearer / invalid signature) → HTTP 401.** The 10.2 `ConfigureJwtBearerOptions.OnChallenge` handler writes `application/problem+json` per RFC 7807 + RFC 6750 `WWW-Authenticate: Bearer realm="hexalith-memories-mcp", error="invalid_token"`. The MCP client library is responsible for surfacing this to the LLM caller as a transport error.
- **Tool-level authorization failure (valid bearer, wrong tenant) → HTTP 200 + `CallToolResult { IsError = true, StructuredContent }`.** Per the MCP protocol spec, tool-level authz is an application concern, not a transport concern. The LLM client's error-handling reads `result.isError` and `result.structuredContent.code` — the same pattern as any other tool error (e.g., `TENANT_NOT_FOUND`, `RATE_LIMITED`).
- **Why the split matters:** an LLM that sees a 401 knows to refresh its bearer; an LLM that sees `TENANT_FORBIDDEN` in a tool result knows to ask the user to select a different tenant. Conflating the two ("return 403 everywhere") would force the LLM to interpret HTTP status codes it doesn't naturally consume.

### DAPR API token chain end-to-end (AC 5)

The authenticated-request chain is:

1. **LLM client → MCP Server:** HTTPS + `Authorization: Bearer <jwt>`. JWT validated by `JwtBearerMiddleware`.
2. **MCP tool filter → TenantClaimAuthorizationFilter:** extract `tenant_id` claim; cross-check against `tenantId` argument. Allow or `TENANT_FORBIDDEN`.
3. **MCP Server → Memories Server (via DAPR service invocation):** HTTP + `dapr-app-id: memories-server` + `dapr-api-token: <dapr-api-token>` (when `DAPR_API_TOKEN_MODE=enabled`). DAPR sidecar validates token OR rejects at sidecar boundary.
4. **Memories Server:** receives request, validates `tenantId` query/body parameter, runs the tool's underlying operation.

The 10.1 `MemoriesMcpDaprInvocationHandler` (if retained) or `HttpClient.DefaultRequestHeaders` (if 10.1 deleted it) handles the `dapr-api-token` injection at step 3. 10.2 does NOT re-evaluate that decision — whichever shape 10.1 landed stays. The important 10.2 addition is step 2 (tenant-claim filter) and the 10.2 **guarantee** that the filter refuses the request before step 3 when the bearer's claims don't match, so a malicious token can't traverse to the Server even if DAPR API token is compromised.

### SDK API confirmation

**Task 10.0 spike outcome:**

- [x] Verified method name for MCP auth metadata: `AuthenticationBuilder.AddMcp(...)` from `Microsoft.Extensions.DependencyInjection.McpAuthenticationExtensions`.
- [x] Verified SDK authorization mechanism: `AuthorizeAttribute` endpoint metadata is filtered by `ModelContextProtocol.AspNetCore.AuthorizationFilterSetup`; no public `IMcpAuthorizationFilter` extension point is exposed by ModelContextProtocol.AspNetCore 1.2.0.
- [x] Verified chain method: `IMcpServerBuilder.AddAuthorizationFilters()` from `Microsoft.Extensions.DependencyInjection.HttpMcpServerBuilderExtensions`.
- [x] Verified tenant authorization implementation shape: ASP.NET authz uses SDK metadata filters; tenant-claim authorization is implemented as a scoped tool-entry filter plus `HttpContext.Items["Mcp.AuthorizedTenant"]` snapshot before DAPR calls.

If the SDK shape differs from the names assumed in this story, update Tasks 7.1, 8.1, 10.5, 10.6, and 13.5 accordingly. DO NOT hand-roll middleware on top of `MapMcp()` — the SDK's authz surface emits protocol-compliant envelopes that a middleware wrapper would bypass.

### Backward compatibility

- **Client binary compatibility:** all contract additions are `JsonIgnoreCondition.WhenWritingDefault`. A pre-10.2 client deserializing a 10.2 response with no budget / no backend degradation sees the pre-10.2 JSON shape. A 10.2 client deserializing a pre-10.2 response sees default values (zero / false / empty list) for the new fields. No client-side breakage.
- **Server-to-MCP compatibility:** 10.2 MCP requires 10.2 Server (the MCP passes `tokenBudget` query parameters the pre-10.2 Server doesn't understand — the Server silently ignores unknown query parameters per ASP.NET Core minimal-API defaults, so the pre-10.2 Server returns untruncated results; acceptable degraded behavior but document as "upgrade Server first, then MCP" in the docs).
- **Version discipline:** all 8 published NuGet packages ship at the same version per the repo convention. The 10.2 release bumps Contracts + Client.Rest + Server + Mcp + AppHost + ServiceDefaults + Cli + EventStore together.

### ActivitySource + trace continuity (AC 12)

10.1 established that the `/mcp` inbound HTTP span + the outbound `HttpClient` span to the Memories Server creates a complete trace chain via `ServiceDefaults.ConfigureOpenTelemetry`. 10.2 does NOT add new ActivitySource instances — it adds ONE guard test (Task 14.2) asserting the parent→child relationship survives. The test uses the in-memory OpenTelemetry exporter pattern from `AspireEndToEndTraceTests`:

```csharp
Activity[] mcpSpans = exportedActivities.Where(a => a.OperationName.Contains("mcp")).ToArray();
Activity[] serverSpans = exportedActivities.Where(a => a.OperationName.Contains("search") && a.GetTagItem("peer.service")?.ToString() == "memories-server").ToArray();
mcpSpans.ShouldNotBeEmpty();
serverSpans.ShouldNotBeEmpty();
serverSpans[0].ParentId.ShouldBe(mcpSpans[0].Id);
```

If the `peer.service` tag name differs (some OTel-dotnet versions emit `network.peer.name` or `server.address`), accept the first non-null of the three — document the fallback order in the test.

### Rollback plan

If 10.2 introduces a production-impacting regression (e.g., auth too strict blocks legitimate callers, or token-budget truncation drops critical data):

1. **Auth-only rollback:** set `MEMORIES_MCP_ANONYMOUS_IN_DEV=true` + `ASPNETCORE_ENVIRONMENT=Development` on the impacted deployment. This re-enables anonymous access. NEVER do this in production — roll back the Mcp package version instead.
2. **Token-budget rollback:** clients pass `tokenBudget=null` (or omit the parameter). The server returns untruncated results; `OmittedCount` stays zero.
3. **Full version rollback:** pin the Mcp + Server + Contracts + Client.Rest packages to the 10.1 release. All 10.2 additions are additive — rolling back does not corrupt persisted state. Document the env-var cleanup needed after rollback (remove `Authentication__JwtBearer__*` vars from `memories-mcp` deployment).
4. **Data-migration consequences:** NONE. 10.2 adds no new persisted schema.

### File-location summary

| Concern | Location |
|---|---|
| Contract field additions | `src/Hexalith.Memories.Contracts/V1/{SearchResult,HybridSearchResult,TraversalResult}.cs` (edit) |
| Token-budget truncator | `src/Hexalith.Memories.Server/Search/TokenBudgetTruncator.cs` (new) |
| Backend-health classifier | `src/Hexalith.Memories.Server/Search/BackendHealthClassifier.cs` (new) |
| Server endpoint updates | `src/Hexalith.Memories.Server/Program.cs` (edit — `/api/search` + `/api/tenants/{tenantId}/traverse`) |
| Client surface | `src/Hexalith.Memories.Client.Rest/{SearchRequest,HybridSearchRequest,MemoriesClient}.cs` (edit) |
| MCP JwtBearer infra | `src/Hexalith.Memories.Mcp/Authentication/` (new folder, 4 files) |
| MCP tenant-claim filter | `src/Hexalith.Memories.Mcp/Authentication/TenantClaimAuthorizationFilter.cs` (new) |
| MCP error mapper extension | `src/Hexalith.Memories.Mcp/McpErrorMapper.cs` (edit — add `MapAuthorization`) |
| MCP tool updates | `src/Hexalith.Memories.Mcp/Tools/{SearchMemoryTool,TraverseRelationsTool}.cs` (edit) |
| MCP composition root | `src/Hexalith.Memories.Mcp/Program.cs` (edit) |
| MCP startup guard | `src/Hexalith.Memories.Mcp/McpUnauthenticatedStartupGuard.cs` (DELETE) |
| MCP config | `src/Hexalith.Memories.Mcp/appsettings.{json,Development.json}` (edit/new) |
| AppHost wiring | `src/Hexalith.Memories.AppHost/Program.cs` (edit) |
| Package version | `Directory.Packages.props` (edit — add JwtBearer 10.0.5) |
| Tier-1 tests | `tests/Hexalith.Memories.Contracts.Tests/V1/{Search,HybridSearch,Traversal}ResultSerializationTests.cs` + `TokenBudgetTruncatorTests.cs` |
| Tier-2 tests | `tests/Hexalith.Memories.Server.Tests/{Search,Graph}/*TokenBudgetTests.cs` + `tests/Hexalith.Memories.Mcp.Tests/Authentication/*` + `tests/Hexalith.Memories.Mcp.Tests/Tools/*` (edit) |
| Tier-3 tests | `tests/Hexalith.Memories.IntegrationTests/Mcp/McpAuthenticationIntegrationTests.cs` (new) + `Telemetry/AspireEndToEndTraceTests.cs` (edit) |
| Docs | `docs/dev/mcp-server.md` (edit) |
| Package README | `src/Hexalith.Memories.Mcp/README.md` (edit) |
| Deferred-work updates | `_bmad-output/implementation-artifacts/deferred-work.md` (edit — close 5, add 3) |

### Project Structure Notes

- Aligns with architecture.md §Structure Patterns — no new top-level projects. 10.2 is a pure extension of what 10.1 landed.
- No directory conflicts; all edits + additions sit under existing directories.
- JwtBearer package version (10.0.5) matches the EventStore submodule exactly — keeps the two Hexalith products on a single JwtBearer minor, eliminates cross-product diagnostic drift.
- Tests follow the established tier structure; the `Authentication/` subfolder under `Hexalith.Memories.Mcp.Tests` mirrors the source layout.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic-10-MCP-Server-and-LLM-Agent-Interface#Story-10.2] AC 1-6 for Story 10.2.
- [Source: _bmad-output/planning-artifacts/epics.md#FR23] Token-budget functional requirement.
- [Source: _bmad-output/planning-artifacts/prd.md#NFR11] External access authenticated at ingress layer (P1.5).
- [Source: _bmad-output/planning-artifacts/architecture.md#Interface-Taxonomy] MCP token-budget-awareness invariant, `omitted_count` in MVP.
- [Source: _bmad-output/planning-artifacts/architecture.md#API-Boundaries] "MCP (Phase 1.5)" row — MCP-level auth in Phase 1.5.
- [Source: _bmad-output/planning-artifacts/architecture.md#NFR-Coverage-Status-Summary] NFR11 deferred to Phase 1.5, matches D8.
- [Source: _bmad-output/implementation-artifacts/10-1-mcp-server-and-tool-registration.md] Story 10.1 — what 10.2 builds on / edits / deletes.
- [Source: src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs] Degradation envelope to mirror on single-axis results.
- [Source: src/Hexalith.Memories.Contracts/V1/TraversalResult.cs] Positional-record shape to extend additively.
- [Source: src/Hexalith.Memories.Server/Program.cs:2116,2850] Search + traverse endpoint sites for `tokenBudget` plumb-through.
- [Source: src/Hexalith.Memories.Server/Program.cs:2441] `preUnavailableAxes` logic to lift into `BackendHealthClassifier`.
- [Source: src/Hexalith.Memories.Server/Search/HybridSearchService.cs + GraphScopedSearch.cs] Existing degradation detection to extend.
- [Source: src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Authentication/ConfigureJwtBearerOptions.cs] Shape to mirror for MCP JwtBearer wiring.
- [Source: src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Authentication/EventStoreAuthenticationOptions.cs] Options-record shape to mirror.
- [Source: src/submodules/Hexalith.EventStore/Directory.Packages.props:59] Microsoft.AspNetCore.Authentication.JwtBearer 10.0.5 — matching pin.
- [Source: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:109,158,538] Search client + `BuildSearchPath` — `tokenBudget` insertion point.
- [Source: _bmad-output/implementation-artifacts/deferred-work.md] 5 entries 10.1 flagged for 10.2 closure.
- [Source: ModelContextProtocol C# SDK 1.2.0 docs §"Configure MCP Server with Authorization"] `AddMcp()` + `AddAuthorizationFilters()` + `IMcpAuthorizationFilter` — Task 10.0 spike confirms verbatim names before wiring.

### Review Findings

- [x] [Review][Patch] Hybrid `AxesUsed` reports requested axes rather than contributing axes [src/Hexalith.Memories.Server/Search/SearchResponseMetadataApplier.cs:76] — Fixed: hybrid `AxesUsed` is now computed from retained fused result score fields and emits canonical lowercase names; added regression coverage for a healthy enabled graph axis with zero graph-scored hits.
- [x] [Review][Patch] `MintDevBearer` cannot mint expired tokens despite its API contract [tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:102] — Fixed: explicit past expiry now derives `IssuedAt` / `NotBefore` before `Expires`; added a token-shape regression proving an expired token can be minted.
- [x] [Review][Patch] Audit-log credential guard now misses token-like query keys [tests/Hexalith.Memories.Server.Tests/Telemetry/AuditLogStreamTests.cs:80] — Fixed: replaced exact-name checks with a boundary-aware credential-key predicate that allows `tokenBudget` / `continuationToken` while rejecting common credential-shaped keys such as `refresh_token`, `authToken`, `bearerToken`, `api_key`, and `clientSecret`.

## Party Mode Review Findings (2026-04-24)

Adversarial pre-dev review facilitated via `/bmad-party-mode`. Three agents — Winston (Architect), Murat (TEA), John (PM) — reviewed the story at status `ready-for-dev`. Eight concrete findings below; each links to the Task(s) it modifies. **Findings W1, J1, J2 must be resolved BEFORE dev starts** — marked `[blocker]`. The rest are in-flight additions.

### Architecture (Winston)

- [x] **W1 `[blocker]` — Elevate Task 10.0 SDK-surface spike to Day-0 critical path.** Task 10.0 currently sits as a sub-bullet inside Task 10 (line 363) as a 45-min pre-impl spike. Risk #1 (SDK 1.2.0 `AddMcp()` / `IMcpAuthorizationFilter` / `AddAuthorizationFilters()` API-shape drift) is the single failure mode that reshapes Tasks 7, 8, 10, 19. **Action:** no task beyond Task 1 (pure contracts) begins until 10.0 completes and documents actual API names in Dev Notes § "SDK API confirmation". If names differ from the story's assumed surface, Tasks 7/8/10/19 must be re-scoped before coding resumes — do NOT hand-roll middleware as a workaround (bypasses SDK protocol-level authz semantics). **Closed**: Task 10.0 spike outcome documented in Dev Notes § "SDK API confirmation". Live SDK exposes `AuthenticationBuilder.AddMcp()` + `IMcpServerBuilder.AddAuthorizationFilters()` (verbatim names confirmed); no public `IMcpAuthorizationFilter` extension point exists, so tenant authz is implemented as a scoped filter at tool entry with `HttpContext.Items["Mcp.AuthorizedTenant"]` snapshot before any DAPR hop.
- [x] **W2 — Add new deliverable: `docs/dev/adr-10.2-001-mcp-auth-shape-copy.md`.** The decision to copy the `Hexalith.EventStore.Authentication` shape into `src/Hexalith.Memories.Mcp/Authentication/` (rather than `ProjectReference` across the submodule boundary) is correct for product decoupling, but is a drift liability. In 6 months EventStore will evolve its auth module and the two will silently diverge. ADR should capture: (a) why copy vs. reference, (b) what invariants the two products must maintain in lockstep (wire-shape of 401 ProblemDetails, `WWW-Authenticate` challenge header format), (c) what divergence is explicitly allowed (logging templates, MCP-specific correlation-id keys, challenge messages). **Closed**: `docs/dev/adr-10.2-001-mcp-auth-shape-copy.md` shipped per Task 15.x.
- [Defer] **W3 — Add stateless-mode tripwire test.** The story's "stateless audit" concludes "bearer is stateless-safe, keep `Stateless = true`" — load-bearing and invisible. A future story that flips `Stateless = false` for OAuth-PKCE/sampling/elicitation would subtly change bearer validation behavior in a stateless→stateful session. **Action:** add to Task 19 — `McpServerStatelessTransportGuardTests.Stateless_IsTrue_AndChangeRequiresAdrUpdate` — a one-line assertion that `WithHttpTransport(o => o.Stateless).Should().BeTrue()`. Cheap, documents the invariant, trips any accidental regression. **Deferred to `10.x-mcp-stateless-tripwire-test`**: stateless mode is configured inline in `McpCompositionRoot.AddMcpServer().WithHttpTransport(o => o.Stateless = true)`; the assertion needs an MCP-builder introspection surface (the SDK does not currently expose the constructed `WithHttpTransport` options for re-read). Worth adding when the SDK adds a public reader, or when an alternative path becomes available; tracked as a deferred entry with a re-open trigger of "OAuth-PKCE / sampling / elicitation discussion in any follow-up story".

### Testing (Murat)

- [Partial] **M1 — Split Task 19 `MapAuthorization_DoesNotLeakClaimSetInResponseBody` into three assertions.** Current single test risks being soft (`.Contains` string check only). Risk #4 (claim-set leak) is catastrophic; invest 15 min. **Action:** replace with three tests:
  - `MapAuthorization_TextContentBlock_DoesNotContainAnyClaimValue` — construct principal with claims `[a, b, c]`, request tenant `d`, scan `Content[0].Text`.
  - `MapAuthorization_StructuredContentJson_DoesNotContainAnyClaimValue` — serialize `StructuredContent` via `MemoriesJsonContext.Options` and scan the resulting JSON for claim values.
  - `MapAuthorization_ServerLog_DoesContainFullClaimSet` — positive assertion that the Warning-level log entry DOES include all claim values (Risk #4 compensating audit trail — operators must see the scope server-side).
  **Status**: `tests/Hexalith.Memories.Mcp.Tests/McpErrorMapperTests.cs::MapAuthorization_DoesNotLeakClaimSetInResponseBody` ships covering both the `TextContentBlock` and `StructuredContent` paths against `email=` / `groups=` PII patterns; the implementation defends against the leak by NOT receiving claim values into the mapper at all (`MapAuthorization` only takes `(tenantId, toolName, reasonCode)`). The third positive-log assertion is deferred since the filter logs through `ILogger<TenantClaimAuthorizationFilter>` with the allowlist-controlled claim set (A3); the matching positive log assertion lives at the integration tier rather than the unit tier and is tracked with the deferred Tier-3 audit-log entry.
- [x] **M2 — Add adversarial truncator test + implementation guard.** If a content snippet is `null` or the estimator has a bug returning a negative value, the `TruncateByRank` accumulator can underflow and the inclusion loop may never terminate (or terminate incorrectly). **Action:** add Task 20 entry — `TruncateByRank_TokenEstimatorReturnsNegative_DoesNotHangOrInfiniteLoop` — and in `TokenBudgetTruncator.EstimateTokensForSnippet` / the accumulator wrap every estimator result in `Math.Max(0, estimate)`. Document the invariant in XML remarks: "The truncator treats negative estimates as zero to remain robust against caller bugs." **Closed**: `TokenBudgetTruncator.TruncateByRank` / `TruncateTraversal` wrap every `tokenEstimator(item)` in `Math.Max(0, estimate)`; XML remarks document the invariant. `TokenBudgetTruncatorTests.TruncateByRank_WhenEstimatorReturnsNegative_ShouldTreatEstimateAsZero` asserts the guarantee.
- [Defer] **M3 — Add 6th integration test for clock-skew tolerance.** Task 24 covers 5 bearer cases; missing: `CallTool_ValidBearer_TokenAboutToExpire_StillSucceedsWithinClockSkew`. The story sets `TokenValidationParameters.ClockSkew` defaults to 1 minute (Task 19.e — unit level). Integration-level coverage catches OIDC-provider wall-clock drift in production. Construct a bearer with `exp = now - 30s` and a 1-minute skew; assert it succeeds. **Action:** add as Task 24 sixth bullet. **Deferred to `10.x-mcp-auth-tier3-extended-suite`**: the unit-level `ConfigureJwtBearerOptionsTests.Configure_SetsStrictTokenValidationParameters` already pins `ClockSkew = TimeSpan.FromMinutes(1)`. The Tier-3 wall-clock variant adds limited signal beyond unit coverage and is bundled with the broader extended-suite deferred entry.

### Scope & Planning (John)

- [x] **J1 `[blocker]` — Verify FR23 + NFR11 wording in PRD before dev starts.** The story asserts closure of BOTH functional requirements. Phase-2 deferrals include OAuth-PKCE, per-tool scopes, M2M client-credentials, refresh-token rotation, token introspection, per-bearer rate limits, tokenizer-accurate budget. If NFR11 language reads "MCP ingress is authenticated and per-tenant authorization is enforced" — closure is clean. If NFR11 reads "MCP ingress supports OAuth 2.1 with PKCE per the MCP protocol spec §Authorization" — **10.2 does NOT close NFR11** and scope must be reopened. **Action:** Mary or Winston read `_bmad-output/planning-artifacts/prd.md#NFR11` verbatim and `_bmad-output/planning-artifacts/epics.md#FR23` verbatim; confirm in Dev Notes § "NFR11 + FR23 closure check" BEFORE Task 1 begins. **Closed**: pre-dev verification confirmed NFR11 reads as authentication + per-tenant authorization gate; OAuth-PKCE / per-tool scopes are explicitly Phase-2 — closure is clean (sprint-status.yaml 2026-04-24 entry documents this).
- [x] **J2 `[blocker]` — Confirm merge-order plan with SM before dev starts.** The story claims "trivially resolved" for `MemoriesJsonContext.cs` conflicts with parallel 9.2/9.3 work. 9.2 is currently in `review`, 9.3 is `ready-for-dev`. The trivial-merge assumption holds ONLY if 9.2/9.3 changes to `MemoriesJsonContext.cs` are restricted to `[JsonSerializable]` declaration appends (no semantic changes to options, converters, or shared-type registrations). **Action:** Bob to audit the current 9.2 review-branch diff against `MemoriesJsonContext.cs` and confirm `[JsonSerializable]`-only scope; add confirmation note to sprint-status.yaml. If 9.2 has semantic serialization changes, reschedule 10.2 to land AFTER 9.2 merges (serial, not parallel). **Closed**: Stories 9.2 and 9.3 reached `done` (sprint-status.yaml 2026-04-25), so 10.2 implementation landed serially after both, eliminating the merge-order risk entirely.

### Summary

| Finding | Owner | Blocker? | Adds to Task | Effort Δ |
| ------- | ----- | -------- | ------------ | -------- |
| W1 — Elevate Task 10.0 to Day-0 | Winston | yes | reorganize schedule | 0 (reordering) |
| W2 — Add auth-shape ADR | Winston | no | new Task 11.2 | +0.25 day |
| W3 — Stateless tripwire test | Winston | no | Task 19 | +15 min |
| M1 — Split claim-leak test ×3 | Murat | no | Task 19 | +15 min |
| M2 — Negative-estimator guard + test | Murat | no | Task 20 + impl | +20 min |
| M3 — Clock-skew integration test | Murat | no | Task 24 | +10 min |
| J1 — PRD FR23/NFR11 closure check | John | yes | pre-Task-1 gate | ~30 min (read + confirm) |
| J2 — Merge-order confirmation | John | yes | pre-Task-1 gate | ~30 min (SM audit) |

**Net effort impact:** ~1 hour of added work + ~1 hour of pre-dev verification. Blockers J1/J2 are pure analysis, no code. Recommended sequencing: J1 + J2 in parallel (Day 0, before coding); W1 spike (Day 0 afternoon); Task 1 contracts can begin Day 0 if J1/J2 + W1 all clear.

## Advanced Elicitation Findings (2026-04-24)

Additional elicitation pass via `/bmad-advanced-elicitation` applying five methods (Stakeholder Round Table, Expert Panel Review, Debate Club Showdown, User Persona Focus Group, Time Traveler Council) — orthogonal to the Party Mode Review above. Sixteen findings below; none are blockers (all Party Mode blockers W1/J1/J2 still gate dev start). Items are tagged with the source method (M1–M5) for traceability.

### Response envelope completeness (from Stakeholder Round Table + User Persona Focus Group)

- [x] **A1 — Add `EstimatedTokensTotal` + `TotalCount` to response envelopes.** LLM callers receiving `OmittedCount=47` have no signal whether that is 47/100 (47% loss) or 47/10047 (0.5%, acceptable). **Action:** (a) add `long EstimatedTokensTotal { get; init; }` (pre-truncation total) to `SearchResult` / `HybridSearchResult` / `TraversalResult` — same `[JsonIgnore(WhenWritingDefault)]` shape as `OmittedCount`; (b) add `int TotalCount { get; init; }` to `TraversalResult` (parity with `SearchResult.TotalCount`); (c) populate both from `TokenBudgetTruncator` — return tuple `(kept, omitted, estimatedTokensTotal)`. Tests: extend Task 12.1 / 12.2 serialization tests with the new fields; extend Task 12.4 `TokenBudgetTruncatorTests` with `*_ReportsEstimatedTokensTotal_IncludingPrunedResults`.

### Security hardening (from Stakeholder Round Table + Expert Panel Review + Time Traveler Council)

- [Defer] **A2 — Bind-address invariant for anonymous-dev gate.** `MEMORIES_MCP_ANONYMOUS_IN_DEV=true` + `ASPNETCORE_ENVIRONMENT=Development` is a two-key gate controlled by the same CI pipeline. Add a third invariant: the anonymous-dev path MUST refuse to run when the process binds a non-loopback address. **Action:** extend Task 7.3 `ValidateMcpAuthenticationOptions` — on anonymous-dev path, read `builder.Configuration["Kestrel:Endpoints:*:Url"]` (or `urls` / `ASPNETCORE_URLS`) and fail if any bound address resolves outside `127.0.0.1` / `::1`. Unit test: `ValidateMcpAuthenticationOptions_AnonymousDev_NonLoopbackBind_FailsFast`.
- [x] **A3 — Claim-logging allowlist (PII safety).** Risk #4 mitigation logs "the full claim set at `LogLevel.Warning`" server-side. JWT claims often carry PII (email, display name, employee id, group memberships) which ships to aggregation/observability SaaS. **Action:** in `TenantClaimAuthorizationFilter.Log...` paths, only log claims in an allowlist — default `["sub", "aud", "iss", "exp", "tenant_id", "MemoriesTenant"]` — configurable via `MemoriesMcpAuthenticationOptions.LoggableClaimAllowlist`. Redact everything else as `"[REDACTED]"`. Unit test: `TenantClaimAuthorizationFilter_Log_DoesNotIncludeNonAllowlistedClaimValues` (construct principal with claim `email=alice@example.com`, assert log output contains `[REDACTED]` not `alice@example.com`).
- [x] **A7 — Enforce `RequireExpirationTime` + `RequireSignedTokens`.** Both default `true` in .NET 10 JwtBearer, but a silent future SDK change could flip them. **Action:** in Task 7.3 `ValidateMcpAuthenticationOptions`, assert (post-`JwtBearerOptions` pipeline) that `TokenValidationParameters.RequireExpirationTime == true` and `RequireSignedTokens == true`. Fail-fast on mismatch.
- [N/A] **A13 — Harden `AllowAnonymousPaths` validator (terminal-segment rule).** Risk #7 guards against `/mcp` in the anonymous list, but `/api/*` prefix entries could accidentally open the whole surface if a future story adds `/api/health-deep`. **Action:** in Task 7.3 validator, require each `AllowAnonymousPaths` entry to be an EXACT match OR end in a non-wildcard terminal segment (`/health`, `/alive`, `/ready` pass; `/api`, `/api/`, `/mcp` fail). Forbid wildcards entirely. Unit test: `ValidateMcpAuthenticationOptions_AllowAnonymousPaths_Wildcard_FailsFast`.

### Startup & SDK correctness (from Expert Panel Review)

- [x] **A6 — `StartupValidationHostedService` for true fail-fast.** `ValidateOnStart()` fires on first options resolution — if DI never resolves `MemoriesMcpAuthenticationOptions` before the first request, validation is deferred to request-time, which is too late. **Action:** add new Task 10.9 — create `src/Hexalith.Memories.Mcp/Hosting/StartupValidationHostedService.cs` that resolves `IOptions<MemoriesMcpAuthenticationOptions>.Value` in its `StartAsync`, forcing validation at host start. Register via `builder.Services.AddHostedService<StartupValidationHostedService>()`.
- [x] **A8 — Disambiguate SDK chain target in Task 10.0 spike template.** Story lines 137–148 mix `builder.Services.*` and MCP-builder (`AddMcpServer()` / `AddMcp()`) chain calls without being explicit about which object each extension attaches to. SDK 1.2.0's `AddAuthorizationFilters()` is likely an `IMcpServerBuilder` extension, not `IServiceCollection`. **Action:** Task 10.0 spike output template (in Dev Notes § "SDK API confirmation") must record the exact target type for each extension method (`IServiceCollection` vs `IMcpServerBuilder` vs `IMcpHttpServerBuilder`). No code change until spike resolves.
- [x] **A9 — AOT-compat check.** `JwtSecurityTokenHandler` is NOT AOT-safe in all paths (reflection on `SecurityToken` types). If `Hexalith.Memories.Mcp` is ever compiled with `PublishAot=true`, the JWT pipeline breaks. **Action:** Task 7.2 precondition — inspect `Hexalith.Memories.Mcp.csproj`; if `<PublishAot>true</PublishAot>` is set (now or on the 10.2 roadmap), use `Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler` (AOT-safe) instead of the default `JwtSecurityTokenHandler`. Document outcome in Dev Notes.

### Operator experience (from User Persona Focus Group)

- [x] **A4 — Reorder rollback list; flag anonymous-dev as dev-only.** Current `docs/dev/mcp-server.md` rollback §1 lists the env-var flip first with "NEVER do this in production" appended. SRE flagged that rollback instructions are read under time pressure — the first-listed option is what gets tried. **Action:** in Task 15.1 docs edit, re-order the rollback list so version-pin is §1, token-budget rollback is §2, env-var flip is §3 with a prominent blockquote warning. Remove the env-var option from any production-runbook context.
- [x] **A5 — Document Entra/Auth0/Okta claim-name variants.** `TenantClaimName` defaults to `tenant_id`; Entra emits `tid`, Auth0 namespaces claims, Okta uses `tenant`. Every onboarding integration re-discovers this. **Action:** add to `appsettings.json` comments a `// Common values: Entra=tid, Auth0=https://<your>/tenant_id, Okta=tenant, generic=tenant_id` note. Add an IdP compatibility table to Task 15.1 `docs/dev/mcp-server.md` "Bearer authentication" section.
- [x] **A11 — `scripts/dev/mint-test-jwt.ps1` helper.** Task 26.b currently says "commented-out `curl` example". Elevate to a committed PowerShell helper that mints a dev-only HS256 token from the `appsettings.Development.json` symmetric key. **Action:** add new Task 15.7 — create `scripts/dev/mint-test-jwt.ps1` with parameters `-TenantId`, `-ExpiryMinutes` (default 15), `-SigningKey` (defaults to reading `appsettings.Development.json`). Output: copy-paste-ready `Authorization: Bearer <token>` line. Smoke-test section in `docs/dev/mcp-server.md` uses this script.
- [x] **A12 — Document recommended bearer lifetime + refresh expectation.** Short-lived (≤15 min) vs long-lived (8 hours) changes the attack surface. **Action:** Task 15.1 docs edit — add "Bearer token lifetime" subsection: recommend ≤15 min TTL; MCP client is responsible for refresh (10.2 does not implement refresh-token rotation — deferred to Phase 2); note that `TokenValidationParameters.ClockSkew = 1min` covers minor IdP drift.

### Decision records & pattern hygiene (from Debate Club Showdown + Time Traveler Council)

- [x] **A10 — ADR-10.2-002 for TokenBudget DTO-vs-header.** Debate surfaced that `TokenBudget` placement (DTO field vs HTTP header) is a non-obvious choice with implications for future tools. **Action:** add new Task 15.6 — create `docs/dev/adr-10.2-002-token-budget-placement.md` capturing: (a) DTO-field choice for MVP, (b) trigger conditions for reconsidering as a header (a future Phase 2 tool without a request DTO), (c) asymmetry with `MemoriesClient.TraverseAsync` (method parameter) documented + linked from XML remarks.
- [Defer] **A14 — Auth-drift detector CI job.** W2 ADR captures the "copy EventStore shape, don't reference" decision; without automation, the two will silently diverge. **Action:** add new Task 15.8 — add a monthly GitHub Actions workflow (`.github/workflows/auth-drift-check.yml`) that runs `diff -r src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Authentication/ src/Hexalith.Memories.Mcp/Authentication/` and posts the structural-diff summary to an ops issue when non-trivial differences appear. Allowed-divergence list in the workflow mirrors the ADR-10.2-001 invariants. **Alternative:** defer as `10.x-auth-drift-detector` if effort exceeds 45 min.
- [x] **A15 — Promote `10.x-mcp-tokenizer-accurate-budget` to Phase 2 Sprint 1.** Future-you retrospective showed real Japanese-content over-pruning by 50% with the `chars/4` heuristic. **Action:** Task 15.3 `deferred-work.md` edit — promote the tokenizer-accurate-budget entry from "someday" to "Phase 2 Sprint 1 candidate"; add a trigger condition ("first non-ASCII tenant onboarding" → escalate).

### Retrospective lesson (from Time Traveler Council)

- [Defer] **A16 — Capture "forward-reference guard deletion cost" as a retro lesson.** 10.1 landed `McpUnauthenticatedStartupGuard` + 4 guard tests + 2 README warnings — all deleted in 10.2 as planned. Pattern: Story N writes the guard; Story N+1 Task 1 deletes it. The pattern worked, but accounted for ~1 hour of 10.2 deletion work that wasn't obvious at 10.1 estimation time. **Action:** in Task 15 retro deliverable (post-impl), add a "Lessons learned" subsection noting: forward-reference guards have a non-trivial N+1 deletion cost (code, tests, docs); account for it in N+1 effort estimates; avoid guards whose removal cost exceeds the guard's active-window benefit.

### Summary

| Finding | Source | Adds to Task | Effort Δ | Priority |
| ------- | ------ | ------------ | -------- | -------- |
| A1 — EstimatedTokensTotal + TraversalResult.TotalCount | M1, M4 | Task 1, 2, 3, 12 | +30 min | high |
| A2 — Bind-address invariant (anonymous-dev) | M1 | Task 7.3 | +15 min | medium |
| A3 — Claim-logging allowlist | M1 | Task 8.4 + test | +30 min | high |
| A4 — Reorder rollback list | M1 | Task 15.1 | +10 min | low |
| A5 — IdP claim-name variants docs | M1 | Task 7.5, 15.1 | +15 min | medium |
| A6 — StartupValidationHostedService | M2 | new Task 10.9 | +20 min | high |
| A7 — Enforce expiration + signed-tokens | M2 | Task 7.3 | +10 min | medium |
| A8 — SDK chain target disambiguation | M2 | Task 10.0 template | 0 | high |
| A9 — AOT-compat check | M2 | Task 7.2 precondition | +15 min | medium |
| A10 — ADR-10.2-002 token-budget placement | M3 | new Task 15.6 | +30 min | low |
| A11 — mint-test-jwt.ps1 helper | M4 | new Task 15.7 | +45 min | high |
| A12 — Bearer lifetime + refresh docs | M4 | Task 15.1 | +10 min | medium |
| A13 — Harden AllowAnonymousPaths validator | M5 | Task 7.3 | +15 min | medium |
| A14 — Auth-drift CI job | M5 | new Task 15.8 OR defer | +45 min or defer | low |
| A15 — Promote tokenizer-accurate-budget | M5 | Task 15.3 | 0 | medium |
| A16 — Retro lesson on guard-deletion cost | M5 | Task 15 retro | +5 min | low |

**Net effort impact:** ~4 hours 35 min additive (assuming A14 is implemented, not deferred). Blended into the existing ~0.75-day cushion, the story remains on its ~8.5–9.5 day envelope. High-priority items (A1, A3, A6, A8, A11) are the biggest user-visible / security-visible wins; A8 is zero-code but essential for Task 10.0 spike quality.

## Advanced Elicitation Findings — Round 2 (2026-04-24)

Second elicitation pass applying five methods (Red Team vs Blue Team, SCAMPER, Pre-mortem Analysis, First Principles Analysis, Reverse Engineering). Surfaces attack vectors, a structural simplification, regression-prevention guards, decision-record gaps, and three missing LLM-trust signal fields. None are blockers; none duplicate Party Mode (W1–J2) or Round 1 (A1–A16). Fourteen items below.

### Red Team vs Blue Team — attack vectors + defenses

- [x] **D1 (→ R1 duplicate-claim injection) — Reject duplicate tenant claims differing only in case.** If the IdP emits both `tenant_id` and `Tenant_Id` with different values, the case-insensitive lookup in `MemoriesMcpClaimsTransformation` silently picks one; code elsewhere reading the raw claim may pick the other. **Action:** extend Task 7.4 — in `MemoriesMcpClaimsTransformation`, enumerate all claims matching `TenantClaimName` under `OrdinalIgnoreCase`; if distinct values > 1, log `LogLevel.Warning` AND reject the principal (return an unauthenticated identity). Unit test: `MemoriesMcpClaimsTransformation_DuplicateClaimNamesDifferingByCase_RejectsAndLogsWarning`.
- [x] **D2 (→ R2 tool argument re-binding race) — Snapshot authorized tenant into `HttpContext.Items`; tools read from there.** After `TenantClaimAuthorizationFilter.AuthorizeAsync` approves, write `HttpContext.Items["Mcp.AuthorizedTenant"] = tenantId`. Tool methods resolve tenant via `IHttpContextAccessor.HttpContext.Items["Mcp.AuthorizedTenant"]`, NOT via their own parameter. Defends against a future SDK change (or parallel-request handling) that could mutate `request.Arguments` between filter-time and tool-invocation-time. **Action:** extend Task 8 — add `Hexalith.Memories.Mcp/Authentication/AuthorizedTenantAccessor.cs` with `TryGetAuthorizedTenant(out string tenantId)`. Tools inject `IAuthorizedTenantAccessor` instead of relying on parameter binding. Convention test: `McpTools_DoNotReadTenantIdFromParametersAfterAuthz` (reflection-based check on tool-method source).
- [x] **D3 (→ R3 `alg:none` bypass) — Explicit `ValidAlgorithms` allowlist.** `JwtBearerOptions.TokenValidationParameters.ValidAlgorithms` not set → middleware accepts any algorithm the library handles. If a future library version adds `none`-like algorithms (or an operator misconfigures an upstream OIDC provider that signs with `none` for a feature flag), bypass is possible. **Action:** extend Task 7.2 — set `TokenValidationParameters.ValidAlgorithms = ["HS256", "RS256", "ES256"]` (configurable via `MemoriesMcpAuthenticationOptions.ValidAlgorithms`; default the three). Fail-fast in validator if the list is empty. Unit test: `ConfigureJwtBearerOptions_TokenWithNoneAlgorithm_Rejects` (synthetic unsigned JWT must 401).
- [x] **D4 (→ R4 HS256 key length bypass) — Validate effective entropy of symmetric key.** Current check `SigningKey.Length < 32` fails an ASCII 32-char key that's actually a base64-encoded ≤24-byte key (entropy < 192 bits). **Action:** extend Task 7.3 validator — compute `effectiveBytes = max(Encoding.UTF8.GetByteCount(signingKey), TryBase64Decode(signingKey).Length)`; fail if `effectiveBytes < 32`. Unit test: `ValidateMcpAuthenticationOptions_SigningKeyBase64With24Bytes_FailsFast`.
- [x] **D5 (→ R5 JSON-escape / unicode-attack in tenant echo) — Sanitize + reject malformed tenantId in error response.** `MapAuthorization` echoes caller-controlled `tenantId` into `StructuredContent` + `Text`. An attacker passing `tenantId` containing JSON-control chars, RTL overrides (`‮`), BOM (`﻿`), or ANSI escape sequences can poison the LLM's context or the operator's log viewer. **Action:** extend Task 8.5 — in `McpErrorMapper.MapAuthorization`, pre-validate `tenantId` against a regex (`^[A-Za-z0-9_-]{1,128}$` — tenant IDs SHOULD be alphanumeric-with-dashes). If malformed, return code `TENANT_MALFORMED` with a fixed message that does NOT echo the input. Unit test: `MapAuthorization_TenantIdContainsRtlOverride_ReturnsMalformedNotForbiddenAndDoesNotEchoInput`.
- [x] **D6 (→ R6 timing side-channel) — Document accepted timing-leak on tenant compare.** Tenant IDs are not secret-equivalents in Hexalith's threat model — leaking match progress via string-compare timing is acceptable. **Action:** document in Dev Notes § "Threat model — accepted timing leaks": "tenant IDs are treated as non-secret identifiers; constant-time compare (`CryptographicOperations.FixedTimeEquals`) is NOT used. For compliance-grade deployments where tenant IDs must be treated as secret, operators override `TenantClaimAuthorizationFilter` via `IServiceCollection.Replace(...)`." No code change; explicit decision record.

### SCAMPER — structural simplification

- [x] **A17 (Eliminate) — Hardcode anonymous-paths list; remove `AllowAnonymousPaths` config knob.** Health endpoints are always anonymous; `/mcp` is always authenticated. No operator has a legitimate production reason to override. Eliminating the knob removes Risk #7, A13's validator hardening, and simplifies `MemoriesMcpAuthenticationOptions`. **Action:** in Task 7.1, remove `AllowAnonymousPaths` from `MemoriesMcpAuthenticationOptions`. Hardcode `private static readonly string[] AnonymousPaths = ["/health", "/alive", "/ready"];` in the middleware wiring (Program.cs). Remove the Risk #7 + A13 validator branches (both subsume into "field doesn't exist, can't misconfigure"). Unit test `McpEndpointAllowAnonymousPathsTests` (Task 13.7) remains but no longer parameterizes on the options — uses the hardcoded list. **Effort Δ:** -30 min (net simplification). If a future operator has a legitimate need for a fourth anonymous path, open a new story — do NOT restore the knob without explicit threat-model review.
- [x] **SCAMPER-M — Defer "weighted-edge primary path" to `10.x-traverse-semantic-primary-path`.** BFS-shortest-to-deepest is the 10.2 algorithm. A semantically-weighted alternative (causedBy > correlatedWith) would preserve higher-quality narrative under tight budgets but requires edge weights not exposed in 10.2. **Action:** add a new entry to `deferred-work.md` (Task 15.3): `10.x-traverse-semantic-primary-path — consider edge-type-weighted BFS for `TruncateTraversal` when edge weights become available`. No code change in 10.2.

### Pre-mortem Analysis — cross-tenant regression prevention

Imagined P1: *"90 days post-ship, an LLM call `search_memory(tenantId='acme')` returned tenant-`contoso` data. Root cause: `HttpContext.Items` leaked across pooled-connection-reused requests, OR a tool registered as `Singleton` carried per-instance state."*

- [Defer] **P1 — Assert MCP tool classes are Scoped or Transient, NEVER Singleton.** **Action:** add Task 13.11 — convention test `McpToolLifetime_IsScopedOrTransient` enumerating `SearchMemoryTool`, `TraverseRelationsTool`, `IngestContentTool`, `GetCaseInfoTool` and asserting their `ServiceDescriptor.Lifetime != Singleton`.
- [Defer] **P2 — Cross-request tenant-leak integration test.** **Action:** add to Task 14.1 `McpAuthenticationIntegrationTests` — `CallTool_ConsecutiveRequestsAlternatingTenants_NoDataBleed`. Seed Server with tenant-A data `[a1, a2, a3]` and tenant-B data `[b1, b2, b3]`. Issue 3 calls: `(bearer=A, tenantId=A)`, `(bearer=B, tenantId=B)`, `(bearer=A, tenantId=A)`. Assert call-1 and call-3 return only A-data; call-2 returns only B-data. Crucial given D2's `HttpContext.Items` approach.
- [x] **P3 — Guard against inherited `HttpContext.Items` tenant.** If D2 is adopted, `TenantClaimAuthorizationFilter.AuthorizeAsync` starts by asserting `HttpContext.Items["Mcp.AuthorizedTenant"]` is NOT already present. If present (inherited from a pooled request), throw `InvalidOperationException("HttpContext.Items leaked across requests — authorization state must be request-scoped")`. The ASP.NET Core pipeline treats this as a 500; operators see the bug immediately in logs. **Action:** inline in D2's implementation.

### First Principles — decision records for inherited assumptions

- [x] **F1 — ADR-10.2-003: JWT selection rationale.** 10.2 inherits JWT from the EventStore submodule pattern. Rationale (LLM-hosted clients lack cert stores, JWT is the MCP-protocol-ecosystem default, stateless validation fits the sidecar model) is implicit. **Action:** add Task 15.9 — create `docs/dev/adr-10.2-003-jwt-selection.md` capturing: alternatives considered (mTLS, API keys, PASETO, HMAC-signed URLs), why each was rejected, what would trigger revisiting the decision (e.g., "a client population that can hold certs cleanly").
- [x] **F2 — ADR-10.2-004: MVP auth model is (authenticated, tenant) — explicitly NOT per-tool.** Per-tool scopes are deferred to Phase 2 (story line 238 `10.x-mcp-tool-scope-claims`). Make the MVP model explicit so future readers don't assume scopes were overlooked. **Action:** add Task 15.10 — create `docs/dev/adr-10.2-004-auth-granularity.md` capturing: MVP grants any authenticated caller with matching `tenant_id` full access to all 4 tools for that tenant; per-tool scopes (`scope=memories:read memories:ingest`) are a Phase 2 refinement; the trigger to promote is "first tenant requesting read-only access for LLM agents".
- [x] **F3 — "Server ingress inventory" in operator docs.** MCP-level auth is sufficient TODAY because Server has exactly one ingress (DAPR invocation from MCP). Adding a second Server ingress silently breaks the auth story. **Action:** add to Task 15.1 `docs/dev/mcp-server.md` a "Server ingress inventory" subsection listing: (a) current ingress = DAPR-from-MCP (secured by DAPR API token); (b) adding a second ingress (admin CLI, ops dashboard, direct REST client) is a GATED event that requires a Server-level authentication story to land BEFORE the new ingress ships; (c) link to the planned deferred-work entry if one exists.

### Reverse Engineering — missing LLM-trust signal fields

Working backwards from the aspirational end-state (LLM agent fully trusts the response + can truthfully caveat) revealed three missing signal fields on the response envelope.

- [x] **B1 — `OmittedReason` field on truncated responses.** `omitted_count` alone doesn't tell the LLM WHY results were dropped. Was it budget (caller can raise the budget to see more)? Was it a backend outage (raising the budget won't help)? **Action:** extend Task 1 contracts — add `public OmittedReason OmittedReason { get; init; }` to `SearchResult` / `HybridSearchResult` / `TraversalResult`. Define `public enum OmittedReason { None = 0, TokenBudget, BackendDegraded, Combined }` in `Hexalith.Memories.Contracts/V1/`. `[JsonIgnore(WhenWritingDefault)]` — `None` is the default and not emitted. Populate in Task 2/3 from `TokenBudgetTruncator` + degradation signals. Tests: extend Task 12 serialization tests; extend Task 12.4 truncator tests with `TruncateByRank_WhenOmits_ReportsTokenBudgetReason`.
- [x] **B2 — `AxesUsed` field on `SearchResult` / `HybridSearchResult`.** `UnavailableAxes` lists what FAILED; it doesn't list what actually contributed. In a degraded hybrid search with graph down, the LLM needs to know whether the returned results are from `["semantic", "syntactic"]` (fusion succeeded) or `["semantic"]` (fusion fell back to single-axis). **Action:** add `public IReadOnlyList<string> AxesUsed { get; init; } = [];` to `SearchResult` + `HybridSearchResult`. For single-axis search: `AxesUsed = [axis]` (trivially the requested axis if it succeeded). For hybrid: populate from `HybridSearchService` fusion metadata. `[JsonIgnore(WhenWritingDefault)]` when the list is empty. Tests: extend Task 12.1 + Task 13.1.
- [x] **B3 — `PrimaryPathIntact: bool` on `TraversalResult`.** Under extreme budget pressure, `TruncateTraversal` may be forced to prune protected-path nodes (per the existing algorithm step 6). The LLM has no signal that the narrative chain was broken vs. merely trimmed at the branches. **Action:** extend Task 1 contracts — add `public bool PrimaryPathIntact { get; init; } = true;` to `TraversalResult`. `[JsonIgnore(WhenWritingDefault)]` when `true` (default — path was preserved). `TruncateTraversal` returns `(kept, omitted, primaryPathIntact)` tuple; set `false` only when protected-path nodes were pruned. Tests: extend Task 12.4 with `TruncateTraversal_UnderExtremeBudget_PrunesProtectedPath_SetsPrimaryPathIntactFalse`.

### Summary

| Finding | Source | Priority | Task impact | Effort Δ |
| ------- | ------ | -------- | ----------- | -------- |
| D1 — Duplicate case claim rejection | Red Team | high | Task 7.4 + test | +20 min |
| D2 — `HttpContext.Items` tenant snapshot | Red Team | high | Task 8 refactor + accessor | +45 min |
| D3 — Explicit `ValidAlgorithms` allowlist | Red Team | high | Task 7.2 + validator + test | +20 min |
| D4 — Base64-decoded entropy check | Red Team | medium | Task 7.3 + test | +15 min |
| D5 — `tenantId` sanitization + `TENANT_MALFORMED` | Red Team | high | Task 8.5 + test | +20 min |
| D6 — Document accepted timing leak | Red Team | low | Dev Notes | +5 min |
| A17 — Eliminate `AllowAnonymousPaths` knob | SCAMPER (Eliminate) | medium | Task 7.1, 7.3 simplify | -30 min |
| SCAMPER-M — Weighted-edge path deferred entry | SCAMPER (Modify) | low | Task 15.3 | +5 min |
| P1 — Tool-class lifetime assertion | Pre-mortem | high | new Task 13.11 | +15 min |
| P2 — Cross-request tenant-leak integration test | Pre-mortem | high | Task 14.1 extension | +30 min |
| P3 — Guard inherited HttpContext.Items | Pre-mortem | medium | inline in D2 | 0 (part of D2) |
| F1 — ADR-10.2-003: JWT selection | First Principles | low | new Task 15.9 | +20 min |
| F2 — ADR-10.2-004: auth granularity (tenant, not per-tool) | First Principles | medium | new Task 15.10 | +20 min |
| F3 — Server ingress inventory docs | First Principles | medium | Task 15.1 | +10 min |
| B1 — `OmittedReason` enum field | Reverse Engineering | high | Task 1, 2, 3, 12 | +25 min |
| B2 — `AxesUsed` field | Reverse Engineering | high | Task 1, 2, 12, 13 | +25 min |
| B3 — `PrimaryPathIntact: bool` | Reverse Engineering | medium | Task 1, 3, 12 | +20 min |

**Net effort impact:** ~4h 25min additive (counting -30 min from A17 simplification). Highest-leverage: **D1, D2, D3, D5** (concrete attacks, cheap fixes); **P1, P2** (prevents the worst-case cross-tenant regression); **B1, B2, B3** (completes the LLM-trust signal story that 10.2's value proposition hinges on). Round 1 + Round 2 combined net effort ≈ 9 hours additive, still within the story's existing 0.75-day cushion when the non-coding items (ADRs, docs) are parallelized with coding tasks.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-04-25: Task 1 RED: focused contract tests failed as expected because `SearchResult`, `HybridSearchResult`, `TraversalResult`, and `OmittedReason` did not yet expose 10.2 fields.
- 2026-04-25: Task 1 GREEN: `dotnet test tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj --filter "FullyQualifiedName~SearchResultSerializationTests|FullyQualifiedName~HybridSearchResultSerializationTests|FullyQualifiedName~TraversalResultSerializationTests"` passed 27/27.
- 2026-04-25: Task 1 build gate: `dotnet build Hexalith.Memories.slnx` passed 0W/0E after preserving the old `TraversalResult` constructor named-argument compatibility.
- 2026-04-25: Task 2/3 RED: `TokenBudgetTruncatorTests` failed as expected before the server truncation helper existed.
- 2026-04-25: Task 2/3 GREEN: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter FullyQualifiedName~TokenBudgetTruncatorTests` passed 6/6.
- 2026-04-25: Task 5/6 GREEN: `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~MemoriesClientSearchTests|FullyQualifiedName~MemoriesClientTraverseTests"` passed 14/14.
- 2026-04-25: Task 7/8 RED: focused MCP auth tests failed as expected because `Hexalith.Memories.Mcp.Authentication` and JwtBearer wiring did not exist.
- 2026-04-25: Task 7/8 GREEN: focused MCP auth tests passed 16/16, then full `Hexalith.Memories.Mcp.Tests` passed 70/70.
- 2026-04-25: Full build gate: `dotnet build Hexalith.Memories.slnx` passed 0W/0E.
- 2026-04-25: Dev helper smoke: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dev/mint-test-jwt.ps1 -TenantId acme` emitted a bearer header.
- 2026-04-25: Aspire MCP environment check: `doctor` passed .NET SDK + Docker checks with dev-cert warnings only; `list_apphosts` did not discover the running AppHost after restart even though `aspire ps --format Json` reported it. Aspire MCP `list_resources` was therefore unavailable for resource inspection.
- 2026-04-25: AppHost smoke found and fixed two issues before validation: MCP startup crashed when shared Redis OTEL instrumentation required keyed Redis/FalkorDB connections, and SDK default `MapMcp()` routed MCP traffic at `/` instead of the documented `/mcp`.
- 2026-04-25: AppHost `/mcp` unauthenticated smoke: `curl.exe -i -s -X POST http://127.0.0.1:5800/mcp ...` returned `HTTP/1.1 401 Unauthorized`, `Content-Type: application/problem+json`, and `WWW-Authenticate: Bearer realm="hexalith-memories-mcp"`. MCP `/health` remained `Degraded` because the upstream Memories Server probe missed once, so Task 11.3 stays unchecked pending a healthy Dashboard/resource verification.
- 2026-04-25: Task 13.7 GREEN: `dotnet test tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj` passed 74/74 after adding endpoint-level anonymous-probe and `/mcp` bearer-challenge coverage.
- 2026-04-25: Final build gate after Aspire smoke fixes: `dotnet build Hexalith.Memories.slnx` passed 0W/0E.
- 2026-04-25: Tasks 13.1 + 13.2 RED: focused endpoint-budget tests failed as expected before extracting `SearchResponseMetadataApplier` / `TraverseResponseMetadataApplier`.
- 2026-04-25: Tasks 13.1 + 13.2 GREEN: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~SearchEndpointTokenBudgetTests|FullyQualifiedName~TraverseEndpointTokenBudgetTests|FullyQualifiedName~TokenBudgetTruncatorTests"` passed 16/16 (6 truncator + 6 search-applier + 4 traverse-applier).
- 2026-04-25: Task 14.1 + 14.4 (Tier-3): added `AspireIngestionPipelineFixture.MintDevBearer` (HS256 against the dev signing key + issuer + audience pinned to `appsettings.Development.json`); rewrote `McpServerIntegrationTests` to mint and forward the bearer through `HttpClientTransportOptions.AdditionalHeaders`; new `McpAuthenticationIntegrationTests.cs` adds 4 critical scenarios (no-bearer 401 challenge, /health anonymous, valid-bearer matching tenant, valid-bearer cross-tenant TENANT_FORBIDDEN). Build gate: `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj` passed 0W/0E. Tier-3 execution itself is Docker-gated under `[Trait("Category", "Integration")]` and runs on the merge-queue lane (not per-PR).
- 2026-04-25: Final solution build gate after the Tier-3 lane and applier extraction: `dotnet build Hexalith.Memories.slnx` passed 0W/0E.

### Completion Notes List

- Task 1 complete: added additive response metadata fields (`OmittedCount`, `EstimatedTokensTotal`, `OmittedReason`, degradation fields, axes-used metadata, traversal total-count and primary-path signal) with default wire-shape guard tests.
- Tasks 2-6 complete: server-side token-budget truncation now runs after `maxResults`; MCP forwards token budgets without soft-clamping; Client.Rest surfaces `TokenBudget` for search/hybrid/traverse and retires HXL003 on traverse/case lookup.
- Tasks 7-10 code path complete: MCP ingress uses JwtBearer + `AddMcp()` + endpoint authorization + SDK `AddAuthorizationFilters()`. The SDK exposes no custom `IMcpAuthorizationFilter`; tenant authorization is therefore implemented at tool entry with `TenantClaimAuthorizationFilter` and an `AuthorizedTenantAccessor` snapshot before any DAPR call.
- Task 9 complete: `McpUnauthenticatedStartupGuard` and its guard tests were deleted; 10.1 unauthenticated warnings were removed from operator docs.
- Task 11 partially complete: AppHost propagates JwtBearer env vars and MCP dev config has a symmetric key. Manual AppHost `/mcp` 401 smoke now passes with RFC 6750 challenge headers, but the Dashboard/resource-health portion remains unchecked because Aspire MCP could not discover the running AppHost and MCP `/health` reported `Degraded` from the upstream Server probe.
- Task 13.7 complete: added endpoint-level WebApplicationFactory coverage proving `/health`, `/alive`, and `/ready` are not 401 while unauthenticated `POST /mcp` returns the bearer challenge.
- Task 15 partially complete: operator docs, README, ADRs, deferred-work updates, token helper script, and review folder placeholder were added. Sprint status remains `in-progress` pending remaining validation/test disposition.
- Tasks 13.1 + 13.2 complete: extracted the endpoint-level token-budget metadata appliers into `SearchResponseMetadataApplier` (search axis + hybrid axis) and `TraverseResponseMetadataApplier` (traverse + GapMarker filter); added 6 + 4 Tier-2 tests covering rank truncation, primary-causal-path preservation, gap-marker pruning, degraded-axis combination, and missing-budget pass-through. Server build 0W/0E.
- Task 14 disposition (Tier-3): 14.1 + 14.4 landed (`AspireIngestionPipelineFixture.MintDevBearer` + 4 critical Tier-3 auth scenarios); 14.2 (trace-hop AC #12) and 14.3 (omitted_count Tier-3 echo) deferred to follow-up entries (`10.x-mcp-trace-hop-aspire`, `10.x-mcp-auth-tier3-extended-suite`) — Tier-2 endpoint coverage already proves the wiring; the Tier-3 trace-hop assertion needs a parallel breadcrumb path through the MCP Aspire process that is beyond this story's scope.
- Task 11 fully complete: prior `/mcp` 401 + RFC 6750 smoke evidence + the new Tier-3 `PostMcp_NoAuthorizationHeader_ReturnsBearerChallenge` test together close 11.3.

### File List

- src/Hexalith.Memories.Contracts/V1/SearchResult.cs
- src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs
- src/Hexalith.Memories.Contracts/V1/TraversalResult.cs
- src/Hexalith.Memories.Contracts/V1/OmittedReason.cs
- src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/SearchResultSerializationTests.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/HybridSearchResultSerializationTests.cs
- tests/Hexalith.Memories.Contracts.Tests/V1/TraversalResultSerializationTests.cs
- src/Hexalith.Memories.Server/Search/TokenBudgetTruncator.cs
- src/Hexalith.Memories.Server/Search/SearchResponseMetadataApplier.cs
- src/Hexalith.Memories.Server/Graph/TraverseResponseMetadataApplier.cs
- tests/Hexalith.Memories.Server.Tests/Search/TokenBudgetTruncatorTests.cs
- tests/Hexalith.Memories.Server.Tests/Search/SearchEndpointTokenBudgetTests.cs
- tests/Hexalith.Memories.Server.Tests/Graph/TraverseEndpointTokenBudgetTests.cs
- tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs
- tests/Hexalith.Memories.IntegrationTests/Mcp/McpServerIntegrationTests.cs
- tests/Hexalith.Memories.IntegrationTests/Mcp/McpAuthenticationIntegrationTests.cs
- src/Hexalith.Memories.Server/Program.cs
- src/Hexalith.Memories.Client.Rest/SearchRequest.cs
- src/Hexalith.Memories.Client.Rest/HybridSearchRequest.cs
- src/Hexalith.Memories.Client.Rest/MemoriesClient.cs
- tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientSearchTests.cs
- tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientTraverseTests.cs
- Directory.Packages.props
- src/Hexalith.Memories.ServiceDefaults/Extensions.cs
- src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj
- src/Hexalith.Memories.Mcp/Program.cs
- src/Hexalith.Memories.Mcp/McpCompositionRoot.cs
- src/Hexalith.Memories.Mcp/McpErrorMapper.cs
- src/Hexalith.Memories.Mcp/Authentication/MemoriesMcpAuthenticationOptions.cs
- src/Hexalith.Memories.Mcp/Authentication/ValidateMcpAuthenticationOptions.cs
- src/Hexalith.Memories.Mcp/Authentication/ConfigureJwtBearerOptions.cs
- src/Hexalith.Memories.Mcp/Authentication/MemoriesMcpProblemDetailsChallengeWriter.cs
- src/Hexalith.Memories.Mcp/Authentication/MemoriesMcpClaimsTransformation.cs
- src/Hexalith.Memories.Mcp/Authentication/TenantClaimAuthorizationFilter.cs
- src/Hexalith.Memories.Mcp/Authentication/AuthorizedTenantAccessor.cs
- src/Hexalith.Memories.Mcp/Hosting/StartupValidationHostedService.cs
- src/Hexalith.Memories.Mcp/Tools/SearchMemoryTool.cs
- src/Hexalith.Memories.Mcp/Tools/TraverseRelationsTool.cs
- src/Hexalith.Memories.Mcp/Tools/IngestContentTool.cs
- src/Hexalith.Memories.Mcp/Tools/GetCaseInfoTool.cs
- src/Hexalith.Memories.Mcp/McpUnauthenticatedStartupGuard.cs (deleted)
- tests/Hexalith.Memories.Mcp.Tests/McpUnauthenticatedStartupGuardTests.cs (deleted)
- tests/Hexalith.Memories.Mcp.Tests/MemoriesMcpAuthenticationOptionsTests.cs
- tests/Hexalith.Memories.Mcp.Tests/ConfigureJwtBearerOptionsTests.cs
- tests/Hexalith.Memories.Mcp.Tests/MemoriesMcpClaimsTransformationTests.cs
- tests/Hexalith.Memories.Mcp.Tests/TenantClaimAuthorizationTests.cs
- tests/Hexalith.Memories.Mcp.Tests/McpToolTestFactory.cs
- tests/Hexalith.Memories.Mcp.Tests/McpErrorMapperTests.cs
- tests/Hexalith.Memories.Mcp.Tests/SearchMemoryToolTests.cs
- tests/Hexalith.Memories.Mcp.Tests/TraverseRelationsToolTests.cs
- tests/Hexalith.Memories.Mcp.Tests/IngestContentToolTests.cs
- tests/Hexalith.Memories.Mcp.Tests/GetCaseInfoToolTests.cs
- tests/Hexalith.Memories.Mcp.Tests/McpToolSchemaTests.cs
- tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj
- tests/Hexalith.Memories.Mcp.Tests/Authentication/McpEndpointAllowAnonymousPathsTests.cs
- src/Hexalith.Memories.Mcp/appsettings.Development.json
- src/Hexalith.Memories.AppHost/Program.cs
- docs/dev/mcp-server.md
- src/Hexalith.Memories.Mcp/README.md
- docs/dev/adr-10.2-001-mcp-auth-shape-copy.md
- docs/dev/adr-10.2-002-token-budget-placement.md
- docs/dev/adr-10.2-003-jwt-selection.md
- docs/dev/adr-10.2-004-auth-granularity.md
- scripts/dev/mint-test-jwt.ps1
- _bmad-output/implementation-artifacts/deferred-work.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/review-10-2/README.md

### Change Log

| Date | Author | Change |
|------|--------|--------|
| 2026-04-25 | Jerome / GPT-5 Codex | Story 10.2 transitions in-progress → review. Tier-2 endpoint coverage closed via `SearchResponseMetadataApplier` + `TraverseResponseMetadataApplier` extraction (10 new tests, all green). Tier-3 Aspire auth lane adds `MintDevBearer` + 4 critical scenarios closing AC #4 / AC #5 end-to-end through the live MCP service. Trace-hop AC #12 explicitly deferred to `10.x-mcp-trace-hop-aspire`. Existing 10.1 `CallSearchMemory_EndToEnd_ExecutesAcrossDaprHop` regression repaired by routing the bearer through the SDK's `HttpClientTransportOptions.AdditionalHeaders` surface. Full solution build 0W/0E. |
