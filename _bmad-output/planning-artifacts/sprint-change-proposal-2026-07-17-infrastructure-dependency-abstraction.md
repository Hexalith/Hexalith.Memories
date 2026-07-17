# Sprint Change Proposal — Infrastructure-Dependency Abstraction (Dapr / Aspire)

- **Date:** 2026-07-17
- **Author:** Correct-Course workflow (Developer navigation)
- **Project:** Hexalith.Memories
- **Trigger type:** Architectural-invariant enforcement (proactive audit, not a failed story)
- **Change scope classification:** **Moderate** (bounded backlog item + one ADR decision + governance note; no PRD/MVP change, no rollback)
- **Review mode:** Batch
- **Directions confirmed with user:**
  - Search/vector/graph backends (RediSearch, Redis Vector, FalkorDB) → **Isolate + Aspire-wire** (accept direct `NRedisStack`/`NFalkorDB` clients *only* inside the boundary project, connections Aspire-injected; flag any leak or hardcoded endpoint).
  - Review mode → **Batch**.

---

## Change Navigation Checklist (executed)

### Section 1 — Understand the Trigger and Context

- **1.1 Triggering story** — [N/A] No triggering story. This is a proactive, cross-cutting architectural audit requested directly: *"the code should not have any infrastructure dependency. Infrastructure is abstracted by DAPR or Aspire. Check the code base for any dependency and create actions to move the dependency to a DAPR or Aspire component."*
- **1.2 Core problem** — [x] Done. Issue type: **architectural-invariant drift check**. Problem statement: the codebase must uphold the invariant that *product code holds no direct infrastructure dependency* — infrastructure is reached only through Dapr building blocks (workflows, actors, state, pub/sub, secrets, service invocation, Conversation API) or Aspire (connection/endpoint discovery, orchestration, component generation). The audit checks whether that invariant holds and produces actions to close any leak.
- **1.3 Initial impact + evidence** — [x] Done. Three parallel codebase scans (Redis/FalkorDB clients; HTTP/external endpoints; config/secrets/Dapr-bypass) swept all of `src/`, then read each hit for classification. Evidence is cited by `file:line` throughout Section 4.

**Headline finding:** the codebase already upholds the invariant to a high degree. There are **no** `new HttpClient()` leaks, **no** inter-service call bypassing Dapr, **no** hardcoded Redis/FalkorDB endpoints in product code, and **no** hardcoded secrets — every real secret flows through the Dapr secret store via `secretKeyRef`. The genuine findings are a small, bounded set of **hardcoded external endpoint literals in the embedding/CLI paths** plus a few **placement/consistency/governance** items.

### Section 2 — Epic Impact Assessment

- **2.1 Current epic** — [x] Done. No epic is in flight that this blocks. The work is best carried as its own tracked spec (see Handoff).
- **2.2 Epic-level changes** — [N/A] No epic scope/acceptance-criteria change required.
- **2.3 Remaining epics** — [x] Done. No downstream epic is invalidated. The embedding-provider epics (13, 15, 19, 23) own the touched files; changes are additive and behavior-preserving.
- **2.4 New/obsolete epics** — [N/A] None.
- **2.5 Resequencing** — [N/A] None.

### Section 3 — Artifact Conflict and Impact Analysis

- **3.1 PRD** — [N/A] No conflict. The invariant is already the PRD/architecture design intent; nothing in the PRD changes.
- **3.2 Architecture** — [!] Action-needed (small, additive). The invariant is *implied* across the architecture doc but never stated as a single enforceable rule, and the accepted deviations are undocumented. Add: (a) a Decision Registry entry codifying the "no infrastructure dependency in product code" invariant and its sanctioned exceptions; (b) a short ADR for the direct-Redis-KV-store policy (Finding F6).
- **3.3 UI/UX** — [N/A] No UI/UX impact. The Web project is clean (zero HttpClient/URL/Dapr usage).
- **3.4 Other artifacts** — [!] Action-needed. `project-context.md` should gain one rule so future agents treat the invariant as load-bearing. `deploy/dapr/components/*` and Aspire/AppHost wiring are already correct and need no change.

### Section 4 — Path Forward Evaluation

- **4.1 Option 1 — Direct Adjustment** — [x] **Viable**. Effort: **Low–Medium**. Risk: **Low**. Address every finding with focused edits inside the owning projects; no restructuring.
- **4.2 Option 2 — Rollback** — [ ] Not viable / unnecessary. Nothing completed needs reverting.
- **4.3 Option 3 — MVP Review** — [ ] Not viable / unnecessary. MVP scope is unaffected.
- **4.4 Selected path** — [x] **Option 1 (Direct Adjustment) + governance note**. Justification: findings are localized and additive; the highest-value fix removes a specific live external deployment host from shared product defaults; the governance note converts an implicit invariant into an enforceable one, preventing future drift at near-zero cost.

### Section 5 — Sprint Change Proposal Components

- **5.1–5.5** — [x] Done. See Sections below and Implementation Handoff.

### Section 6 — Final Review and Handoff

- **6.1–6.2** — [x] Done (this document). **6.3** — [ ] Action-needed (awaiting user approval). **6.4** — [!] Action-needed (add tracking spec; no epic add/remove/renumber). **6.5** — [x] Handoff plan below.

---

## Section 1 — Issue Summary

**Problem statement.** Hexalith.Memories asserts an architectural invariant that infrastructure must be abstracted behind **Dapr** and **Aspire**, and that product code (`Server`, `Cli`, `Mcp`, `Web`, `Client.Rest`) must carry no direct infrastructure dependency. This audit verified that invariant across the entire `src/` tree and identified the residual leaks so they can be moved behind a Dapr or Aspire component.

**How it was discovered.** Direct request to audit the codebase for infrastructure dependencies. Three parallel scans were run and every candidate hit was read and classified against the intended boundary:

- **Boundary projects** (direct infra wiring legitimate): `AppHost`, `Aspire`, `ServiceDefaults`, `Redis`, `EventStore`.
- **Product projects** (direct infra = violation): `Server`, `Cli`, `Mcp`, `Web`, `Client.Rest`.

**Evidence (what the scans confirmed as already-correct):**

- Inter-service calls use Dapr service invocation, not URLs — MCP→Server via `DaprClient.CreateInvokeHttpClient(appId)` (`src/Hexalith.Memories.Mcp/McpCompositionRoot.cs:68`); Server→EventStore via the Dapr sidecar invoke path (`src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs:86-90`).
- Dapr building blocks wired and used: pub/sub (`deploy/dapr/components/pubsub.yaml`; `EventIngestionController.cs:59` + `Program.cs:62`), state store (`statestore.yaml`, `actorStateStore:"true"`; `DaprWorkflowPayloadStore.cs`), actors/workflows (`IActorProxyFactory`, `AddDaprWorkflow`), secret store (`secretstore.yaml`, deny-by-default scoping; secrets resolved by name via `secretKeyRef`).
- Redis/FalkorDB connections flow from Aspire: AppHost injects `ConnectionStrings__redis` / `ConnectionStrings__falkordb` from `EndpointProperty.HostAndPort` (`AppHost/Program.cs:173-178`); ~45+ Server/EventStore classes consume keyed `[FromKeyedServices("redis"|"falkordb")] IConnectionMultiplexer` — none hardcode an endpoint.
- No `new HttpClient()` anywhere; all HttpClients are typed/named via `IHttpClientFactory` with injected/config addresses.

The genuine residuals are enumerated in Section 4.

---

## Section 2 — Impact Analysis

| Dimension | Impact |
|---|---|
| **Epic impact** | None structural. Touched files are owned by embedding-provider epics (13/15/19/23), CLI epic (7), and cross-cutting hosting. No epic added, removed, resequenced, or rescoped. |
| **Story impact** | No in-flight story changes. New work is best tracked as one consolidated spec (`spec-infrastructure-dependency-abstraction`) with a small task set. |
| **Artifact conflicts** | `architecture.md`: add one Decision Registry entry + one short ADR (F6 policy). `project-context.md`: add one invariant rule. PRD/UX: none. Dapr components / Aspire / AppHost wiring: none. |
| **Technical impact** | Low blast-radius edits. **Test-sensitive:** F1/F2 touch embedding-provider defaults whose validation **ordering is a pinned contract** (`EmbeddingProviderDefaultsTests.Validate_OrderingContract_*`) — changes must keep the field values and validation order identical (behavior-preserving refactor). F5 changes DI composition (Server + ServiceDefaults) — requires a boot/health smoke test through AppHost. No tenant-isolation surface is altered (embedding *defaults* are not tenant routing/auth/index selection), so no cross-tenant negative-evidence obligation is triggered; confirm during implementation. |

---

## Section 3 — Recommended Approach

**Selected path: Direct Adjustment (Option 1) + governance note.** Rationale: the invariant already largely holds; the residuals are localized literals and one placement nuance. Fix them in place, make one policy decision explicit (F6), and codify the invariant so it stays enforced.

- **Effort:** Low–Medium (est. ~1.0–1.5 dev-days incl. tests; F1 is the bulk because of the test-pinned static registry).
- **Risk:** Low. All changes are additive/behavior-preserving; no contract shape change (`TenantEmbeddingConfig` V1 fields unchanged), no rollback.
- **Timeline impact:** None to MVP. Fits a single focused spec.
- **Trade-offs considered:** "Push behind abstraction" for search/vector/graph was **declined by user** in favor of "Isolate + Aspire-wire"; the audit confirms those clients are already isolated and Aspire-injected, so no action is required there.

---

## Section 4 — Detailed Change Proposals

Findings are grouped by category and priority. Each carries a before/after or a concrete target. IDs (F1…F9) map to the action plan in Section 5.

### Group A — Hardcoded external endpoints in product code (genuine violations)

#### F1 — Embedding defaults bake a specific external deployment into product code (Priority: **High**)

- **Where:** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:70,72-74`
- **What:** The shared Ollama default `TenantEmbeddingConfig` hardcodes a live ITANEO-operated host and identity provider:

```csharp
// BEFORE (EmbeddingProviderDefaults.cs, inside the Ollama registry entry)
CreateDefaultConfig: () => new TenantEmbeddingConfig
{
    Provider = OllamaProviderName,
    Model = OllamaModelName,
    Dimensions = 2560,
    RateLimitPerMinute = 6000,
    ApiSecretKeyName = "memories-embedding-client-secret",
    BaseUrl = "https://llm.tache.ai",
    AuthMode = OidcClientCredentialsAuthMode,
    OidcTokenEndpoint = "https://auth.tache.ai/realms/tache/protocol/openid-connect/token",
    OidcClientId = "memories-embedding",
    OidcScope = "openid",
    ReindexRequired = false,
}),
```

- **Why it violates the invariant:** the *default* embeds one specific external deployment's infrastructure (service host + OIDC realm endpoint + client id) into shared product code. These belong in configuration supplied by Aspire/appsettings, not in a compiled `const`/default.
- **Recommended action (target state):** source the Ollama default's `BaseUrl` / `OidcTokenEndpoint` / `OidcClientId` / `OidcScope` from configuration. Introduce an options section bound at startup and seed the registry defaults from it:

```jsonc
// AFTER — src/Hexalith.Memories.Server/appsettings.json (Aspire can override per-environment)
"EmbeddingProviders": {
  "Ollama": {
    "BaseUrl": "https://llm.tache.ai",
    "OidcTokenEndpoint": "https://auth.tache.ai/realms/tache/protocol/openid-connect/token",
    "OidcClientId": "memories-embedding",
    "OidcScope": "openid"
  }
}
```

```csharp
// AFTER — bind options and seed the default from config instead of literals
// (EmbeddingProviderDefaults becomes configuration-seeded; the ordering-contract
//  tests and all validation semantics MUST be preserved unchanged.)
public sealed record EmbeddingProviderDefaultsOptions
{
    public OllamaDefaults Ollama { get; init; } = new();
    public GoogleDefaults Google { get; init; } = new();

    public sealed record OllamaDefaults
    {
        public string? BaseUrl { get; init; }
        public string? OidcTokenEndpoint { get; init; }
        public string OidcClientId { get; init; } = "memories-embedding";
        public string OidcScope { get; init; } = "openid";
    }
    public sealed record GoogleDefaults
    {
        public string ApiBaseUrl { get; init; } = "https://generativelanguage.googleapis.com/v1beta/models";
    }
}
```

- **Constraint for the dev agent:** `EmbeddingProviderDefaults` is `static` and its **validation ordering is a pinned contract** (`Validate_OrderingContract_*`). Keep the produced default *values* and the validation order identical; only the *source* of the endpoint strings moves from literal to config. Add a test asserting no endpoint literal remains in the type (guard against regression).

#### F2 — Google embedding endpoint hardcoded as a `const` (Priority: **Medium**)

- **Where:** `src/Hexalith.Memories.Server/Ingestion/GoogleEmbeddingProvider.cs:19`

```csharp
// BEFORE
private const string ApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
```

- **Recommended action:** move the base URL into the provider registry / `EmbeddingProviderDefaultsOptions.Google.ApiBaseUrl` (config-driven), consistent with how Ollama's `BaseUrl` is already a config-sourced field. Google's endpoint is genuinely stable, so keep the current value as the config default — but the literal must not live in the provider class. Inject the resolved base URL into `GoogleEmbeddingProvider` via its options/registry rather than a `const`.

### Group B — CLI endpoint literals (context-allowed; lower priority)

#### F3 — CLI default server endpoint literal (Priority: **Low**)

- **Where:** `src/Hexalith.Memories.Cli/Configuration/DefaultConfigurationSource.cs:12` — `new("http://127.0.0.1:5000/")`, the tier-4 fallback in the flag > env > file > default resolution chain.
- **Context:** the CLI's minimal direct-HTTP adapter is architecturally sanctioned, and the resolution pipeline is already config-driven; this is only the last-resort default.
- **Recommended action:** keep the fallback semantics, but source the literal from an Aspire-provided/config default (e.g. a bound CLI option with this value as its documented default) rather than pinning the host/port in code. Low priority.

#### F4 — CLI local OTLP endpoint literal (Priority: **Low**)

- **Where:** `src/Hexalith.Memories.Cli/Execution/CliTelemetryBootstrap.cs:31` — `LocalDevelopmentOtlpEndpoint = "http://localhost:18889"`, used only when `--telemetry` is passed without `HEXALITH_MEMORIES_OTEL_ENDPOINT` (`:49`).
- **Recommended action:** acceptable as an env-overridable dev default; optionally promote to config for consistency with F3. Lowest priority — telemetry export, not an inter-service traffic leak.

### Group C — Connection lifecycle placement (architectural nuance)

#### F5 — Product Server project constructs the Redis/FalkorDB multiplexer (Priority: **Medium**)

- **Where:** `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs:260-263` (keyed registration) and `:490-497` (`ConnectRequiredMultiplexer` → `ConnectionMultiplexer.Connect(...)`).

```csharp
// BEFORE — in the product Server composition root
builder.Services.AddKeyedSingleton<IConnectionMultiplexer>("redis", (sp, _) =>
    ConnectRequiredMultiplexer(builder.Configuration, "redis"));
builder.Services.AddKeyedSingleton<IConnectionMultiplexer>("falkordb", (sp, _) =>
    ConnectRequiredMultiplexer(builder.Configuration, "falkordb"));
// ...
private static IConnectionMultiplexer ConnectRequiredMultiplexer(IConfiguration configuration, string connectionName)
{
    string connectionString = configuration.GetConnectionString(connectionName)
        ?? throw new InvalidOperationException(
            $"Connection string '{connectionName}' is required. Start the server through AppHost or set ConnectionStrings__{connectionName}.");
    return ConnectionMultiplexer.Connect(connectionString);
}
```

- **Nuance (not a leak):** the endpoint is Aspire-sourced (no hardcoded host/port), and the throw-if-missing guard forces Aspire provisioning. The only issue is that the *connection construction/lifecycle* lives in a **product** project rather than a boundary project.
- **Recommended action (choose one):**
  - **(a) Relocate** the two keyed `IConnectionMultiplexer` registrations + `ConnectRequiredMultiplexer` into `Hexalith.Memories.ServiceDefaults` (the boundary project), so the Server *consumes* the keyed connections it already uses; **or**
  - **(b) Adopt the Aspire client integration** `builder.AddKeyedRedisClient("redis")` (and, since FalkorDB speaks RESP, `AddKeyedRedisClient("falkordb")`) from `Aspire.StackExchange.Redis`, keeping the fail-fast guard.
- **Preserve:** the descriptive "Start the server through AppHost…" failure message and the keyed names `"redis"`/`"falkordb"` (consumed by ~45+ classes). Add an AppHost boot smoke test to confirm both keyed connections resolve.

### Group D — Dapr building-block policy decision (by-design gray area)

#### F6 — Three KV EventStore stores use direct Redis instead of the Dapr state store (Priority: **Decision + ADR**)

- **Where:**
  - `src/Hexalith.Memories.EventStore/RedisPreflightDedupStore.cs:16,41-45` — `StringSet(..., When.NotExists)` atomic reservation + TTL, "fails OPEN".
  - `src/Hexalith.Memories.EventStore/RedisAggregateCaseMappingStore.cs:19,33` — aggregate→case mapping.
  - `src/Hexalith.Memories.EventStore/RedisObservedEventTypeStore.cs:60,95` — observed-event-type set, pipelined batches.
- **Analysis:** all use the **injected** keyed connection (no endpoint leak) and rely on **Redis-native atomic primitives the Dapr state API does not cleanly expose** — `StringSet When.NotExists` for lock/dedup reservation, TTL/`KeyExpire`, hash fields, pipelined batches, Lua `ScriptEvaluate`. `EventStore` is a **boundary project**, so this is not a product-code leak; the only open question is Dapr building-block choice.
- **Recommended action:** **Accept-and-document** (do not migrate). Write a short ADR recording that these three stores deliberately use direct Redis because their atomic-reserve / TTL / pipeline semantics are load-bearing and not portably expressible via the Dapr state building block; add a one-line comment at each site pointing to the ADR. (If a future requirement removes the atomicity dependency, revisit migrating the pure KV mapping/set to `statestore`.)

### Group E — Consistency / labeling / cleanup (cosmetic, prevent future false-flags)

#### F7 — Dapr-platform token env reads are unlabeled (Priority: **Low**)

- **Where:** `src/Hexalith.Memories.Mcp/MemoriesMcpDaprInvocationHandler.cs:43,49` — reads `DAPR_API_TOKEN_MODE` / `DAPR_API_TOKEN` to attach the `dapr-api-token` header.
- **Analysis:** intentional **Dapr platform contract** (the Dapr runtime itself consumes `DAPR_API_TOKEN`; AppHost/K8s inject it). Not a leak.
- **Recommended action:** add an ADR/comment note that these env reads are the sanctioned Dapr-platform token contract, so future audits don't re-flag them.

#### F8 — MCP upstream app-id and EventStore topic via raw env (Priority: **Low**)

- **Where:** `src/Hexalith.Memories.Mcp/McpCompositionRoot.cs:105` (`MEMORIES_MCP_SERVER_APP_ID`); EventStore topic-name resolution (`EnvironmentTopicAttribute.cs:45`, `EventStoreIntegrationServiceCollectionExtensions.cs:105,111`).
- **Analysis:** logical Dapr identifiers (app-id, topic name), AppHost-injected — acceptable, but read via `Environment.GetEnvironmentVariable` rather than bound `IConfiguration`/`IOptions`.
- **Recommended action:** move these reads to injected `IConfiguration`/`IOptions` for consistency and testability. Cosmetic; bundle with F7.

#### F9 — Compatibility-only port constants in the Redis boundary package (Priority: **Low**)

- **Where:** `src/Hexalith.Memories.Redis/RedisPlaceholder.cs:16,19` — `const "6379"/"6380"`, explicitly compat-only (`<remarks>` says "New code should configure backend endpoints directly"), not used to open any connection.
- **Recommended action:** confirm truly unreferenced by connection code and delete on the next owned major to remove the temptation. No urgency.

### Group F — Governance (make the invariant enforceable)

#### F10 — Codify the invariant + sanctioned exceptions (Priority: **Medium**)

- **architecture.md:** add a Decision Registry entry, e.g. **D30 — "No infrastructure dependency in product code"**: product projects (`Server`, `Cli`, `Mcp`, `Web`, `Client.Rest`) reach infrastructure only via Dapr building blocks or Aspire-injected connections/config; direct infra clients live only in boundary projects (`AppHost`, `Aspire`, `ServiceDefaults`, `Redis`, `EventStore`). **Sanctioned exceptions:** (1) direct `NRedisStack`/`NFalkorDB` for search/vector/graph, isolated in the Redis boundary and Aspire-injected; (2) Dapr-platform env contracts (`DAPR_API_TOKEN`, `APP_API_TOKEN`, `DAPR_HTTP_PORT`); (3) the CLI's minimal direct-HTTP adapter. Reference the F6 ADR.
- **project-context.md** (`### Framework-Specific Rules`): add one rule — *"Infrastructure is reached only through Dapr or Aspire; product code must not hardcode infrastructure endpoints/hosts/ports or construct infra clients — see Decision D30 and the sanctioned-exceptions list."*

---

## Section 5 — Implementation Handoff

**Change scope classification: Moderate** — a bounded, tracked backlog item (multiple small edits) plus one ADR decision and a governance note; no fundamental replan.

### Action plan (sequenced)

| # | Action | Finding(s) | Owner | Priority |
|---|---|---|---|---|
| A1 | Config-drive Ollama default endpoint/OIDC (remove `llm.tache.ai` / `auth.tache.ai` / client-id / scope literals); add options binding + regression guard test | F1 | Developer | High |
| A2 | Config-drive Google embedding base URL (remove `const ApiBaseUrl`) | F2 | Developer | Medium |
| A3 | Relocate keyed `IConnectionMultiplexer` construction to ServiceDefaults **or** adopt Aspire `AddKeyedRedisClient`; keep guard + keyed names; add AppHost boot smoke test | F5 | Developer | Medium |
| A4 | Write ADR accepting direct-Redis for the 3 KV EventStore stores; add pointer comments | F6 | Developer + Architect | Decision |
| A5 | Source CLI default endpoint + OTLP dev endpoint from config defaults | F3, F4 | Developer | Low |
| A6 | Label Dapr-platform token env reads; move MCP app-id + EventStore topic to `IConfiguration`/`IOptions`; confirm/schedule `RedisPlaceholder` removal | F7, F8, F9 | Developer | Low |
| A7 | Add Decision Registry D30 + sanctioned exceptions to `architecture.md`; add invariant rule to `project-context.md` | F10 | Developer + Architect | Medium |

### Guardrails (from project-context.md)

- **Contracts:** `TenantEmbeddingConfig` (V1) field shape is unchanged — changes are additive/behavior-preserving. Do not rename/remove fields.
- **Test-pinned:** preserve `EmbeddingProviderDefaultsTests.Validate_OrderingContract_*` (validation order + produced default values). Cover A1/A2 with tests asserting no endpoint literal remains in product types.
- **Tenant isolation:** embedding *defaults* are not tenant routing/auth/index selection — no cross-tenant negative-evidence obligation is expected; confirm during implementation and record if any isolation surface is touched.
- **Verification:** run focused embedding-provider + hosting/composition tests; run an AppHost boot/health smoke for A3. Follow the sandbox test procedure (`dotnet exec` on the xUnit v3 dll, `DiffEngine_Disabled=true`).
- **Line endings:** new/edited `.cs` and `.md` are CRLF in the working tree per `.gitattributes` — apply the repo's CRLF convention to touched files.

### Success criteria

- No infrastructure endpoint/host/port literal remains in product projects (`Server`, `Cli`, `Mcp`, `Web`, `Client.Rest`) except the sanctioned CLI direct-HTTP default sourced from config.
- Redis/FalkorDB connection construction lives in a boundary project; product code only consumes keyed connections.
- The direct-Redis-KV policy (F6) is recorded in an ADR; the invariant + exceptions (F10) are in `architecture.md` and `project-context.md`.
- Full build green (warnings-as-errors) and focused tests pass; AppHost boots and both keyed connections resolve.

### Handoff routing

- **Primary:** Developer agent (Amelia) — implement A1–A3, A5–A6 and draft the A4/A7 text.
- **Consulted:** Architect (Winston) — approve the F6 ADR and the D30 registry entry.
- **Tracking:** register as a single consolidated spec — `_bmad-output/implementation-artifacts/spec-infrastructure-dependency-abstraction.md` — with tasks A1–A7. No epic add/remove/renumber; update `development_status` only if the team elects to make this epic-owned.

---

## Appendix — Validated-Compliant Inventory (no action; recorded for the audit trail)

These were checked and found **correct** — kept here so a future audit doesn't re-open them:

- **No `new HttpClient()`** anywhere in `src/`; all HttpClients are typed/named via `IHttpClientFactory` (`EmbeddingClient`, `OidcTokenProvider`, `UrlContentFetcher`, `MemoriesClient`) with injected/config addresses.
- **Inter-service traffic via Dapr:** MCP→Server (`McpCompositionRoot.cs:68`) and Server→EventStore (`MemoriesServerServiceCollectionExtensions.cs:86-90`) both use Dapr service invocation (app-id, not URL).
- **Dapr building blocks wired + used:** pub/sub, state store (+ actor state), actors, workflows, secret store — components in `deploy/dapr/components/*`, deny-by-default secret scoping.
- **Secrets:** no hardcoded credentials; product code holds only secret *names*, resolved via the Dapr secret store (`secretKeyRef`). `redis://`/`bearer`/`api-key` regex hits are output redaction (security-positive), not secrets.
- **Search/vector/graph (per user "Isolate + Aspire-wire" ruling):** direct `NRedisStack`/`NFalkorDB` clients are isolated to the Redis boundary and consume **Aspire-injected** keyed connections — no hardcoded endpoints; validated-compliant.
- **Aspire/AppHost/ServiceDefaults boundary wiring:** `ConnectionStrings__*` injection, service discovery, Dapr component generation, sidecar/token wiring — all correctly located in boundary projects.
- **Web project:** zero HttpClient/URL/Dapr usage — clean.
