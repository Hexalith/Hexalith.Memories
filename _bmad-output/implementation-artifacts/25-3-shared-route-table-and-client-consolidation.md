---
baseline_commit: deb9dd79109c3c1cdfa9597574487d0ee6a41671
---

# Story 25.3: Shared Route Table & Client Consolidation

Status: review

<!-- Epic: 25 — Architecture Factorization & Code Health. Closes audit finding A21 (High). Behavior-preserving refactor; commit as `refactor(...)`, not `feat`. -->

## Story

As a maintainer,
I want HTTP routes defined once in a shared table and the REST client de-duplicated behind a single generic send/decode path,
so that a route rename cannot silently break consumers and the client's exception surface stops drifting.

## Acceptance Criteria

1. **Single-source route table exists in Contracts.** A `public static class MemoriesRoutes` exists at `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs` (namespace `Hexalith.Memories.Contracts.V1`), exposing every REST route path template as the one source of truth. The Contracts project gains **no** ASP.NET Core / routing / HTTP package reference (it stays `Microsoft.NET.Sdk`, pure library), and the new type is **not** added to `MemoriesJsonContext` (it is not a wire DTO).

2. **Server consumes the table.** All 46 server route registrations across the 7 endpoint classes (`Ingestion`, `TenantLifecycle`, `Export`, `Consistency`, `Cases`, `Search`, `Graph` `Endpoints.cs`) reference `MemoriesRoutes` instead of inline `"/api/…"` string literals. Every produced route template and HTTP verb is **byte-identical** to today's — no path renamed, no `/api/v1/` versioning added (that is Story 25.4). The 6 non-mapping path literals in `TenantAuthorizationMiddleware.cs` (×2) and `InboundRateLimitPartitionFactory.cs` (×4) are either sourced from the same table or explicitly left with a rationale comment.

3. **Client consumes the table.** `MemoriesClient` builds every `api/…` request path from `MemoriesRoutes` (23 route-bearing method groupings / 20 distinct templates, plus the 2 duplicated `relativePath` literals in `ReadWorkflowStatusUriAsync`). No hand-built `$"api/…"` path literal remains in `MemoriesClient.cs`. The leading-slash asymmetry (server `"/api/…"` vs client `"api/…"`) is resolved deliberately so both sides derive from the same template and each side's base-address semantics are preserved.

4. **One generic send/decode replaces the 22× block.** A single generic `SendAsync<T>` (or equivalently named private/protected helper) backs the standard "request → deserialize `T` or throw" methods, eliminating the 22 copy-pasted decode blocks. It is cancellation-aware (rethrows `OperationCanceledException when ct.IsCancellationRequested`), maps non-2xx via `ErrorResponseDecoder` → `MemoriesRemoteException`, and derives the `INVALID_RESPONSE` empty-body / parse-failure messages from `typeof(T).Name`. Special-shape methods (streaming exports, workflow-`Uri`/instance-id readers, `ProbeHealthAsync`, `404 → null` reads, tenant pagination) keep their bespoke behavior via thin wrappers over the shared send/decode — their observable behavior is unchanged.

5. **`TraverseAsync` parameter order corrected — approved breaking reorder.** The corrected `TraverseAsync` puts `CancellationToken` **last** (move `int? tokenBudget = null` before `CancellationToken ct = default`), matching every sibling client method, the server `GET /api/tenants/{tenantId}/traverse` endpoint, and `GraphTraversalService.TraverseAsync`. The stable positional signature is intentionally replaced because retaining both swapped optional-parameter overloads is CS0121-ambiguous. This is an approved breaking API change and requires a `refactor(...)!` / `BREAKING CHANGE:` marker and a major release. Mockability and wire-surface tests still pass.

6. **Route drift guard still passes.** `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs` is updated so its code→doc extraction still resolves all 46 `/api/*` routes now that they come from `MemoriesRoutes` constants rather than inline literals (its regex only matches inline `app.MapX("/…")` today). `docs/operations/route-surface.md` route strings and row count remain unchanged.

7. **Behavior preserved; build green.** All existing tests pass unchanged: client wire-surface tests under `tests/Hexalith.Memories.Cli.Tests/ClientRest/` (exact path + query assertions), server endpoint/auth/rate-limit tests, route-surface & deployment contract tests, MCP `StubMemoriesClient` tool tests, and `MemoriesClientMockabilityContractTests` (class not sealed, public methods stay `virtual`). `dotnet build Hexalith.Memories.slnx` succeeds with **0 warnings, 0 errors**.

8. **No unrelated scope leakage.** No CLI change (Story 25.5). The MCP tool caller and MCP test stub may receive only the signature-order adaptation required by the approved AC5 break; broader MCP consolidation remains Story 25.6. No route versioning, contract renaming, or persistence-DTO split (Story 25.4). No new packages, no controllers, and no submodule edits.

## Tasks / Subtasks

- [x] **Task 1 — Create `MemoriesRoutes` table in Contracts** (AC: 1)
  - [x] Add `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs`: `public static class MemoriesRoutes` in namespace `Hexalith.Memories.Contracts.V1`, ITANEO copyright header (`file="MemoriesRoutes.cs"`), file-scoped namespace, `using`s inside namespace (SA1200), XML `<summary>` on the class and every public member (`TreatWarningsAsErrors=true` fails otherwise).
  - [x] Model the 37 distinct server templates / 20 distinct client templates as one canonical set (they are the same paths). Prefer template strings that carry `{tenantId}`/`{caseId}`/`{memoryUnitId}`/`{instanceId}`/`{memberId}`/`{batchId}`/`{memoryUnitId}` placeholders, plus small `static` builder helpers that fill and `Uri.EscapeDataString`-escape segment values for the client. Decide the leading-slash convention once (see Dev Notes → Design decisions) and expose whatever each consumer needs without duplicating the literal.
  - [x] Do **not** register the type in `MemoriesJsonContext`; do **not** add any package/project reference to `Hexalith.Memories.Contracts.csproj`.
- [x] **Task 2 — Server endpoints consume the table** (AC: 2, 6)
  - [x] Replace inline `"/api/…"` literals in the 7 `*Endpoints.cs` files with `MemoriesRoutes` references, keeping each produced template byte-identical (verify against the route inventory below and `docs/operations/route-surface.md`). (46 registrations replaced.)
  - [x] Preserve the two experimental (`HXL002`) routes `/api/handlers` and `/api/tenants/{tenantId}/handlers/mismatches` and their `X-Memories-API-Experimental` header behavior.
  - [x] Source (or comment-justify) the 6 non-mapping literals in `TenantAuthorizationMiddleware.cs` and `InboundRateLimitPartitionFactory.cs`. (All 6 now reference `MemoriesRoutes.Tenants`/`.Search`/`.ApiPrefix`/`.Ingest`/`.IngestUrl`/`.IngestDirectory`.)
  - [x] Update `RouteSurfaceContractTests` extraction so `EveryMappedApiRoute_IsDocumented`, `DocumentedApiRowCount_EqualsMappedApiRouteCount`, and `NoProcessOperation_IsAbsentFromCodeAndRefutedInDoc` still resolve all 46 routes from the constants. Keep `docs/operations/route-surface.md` unchanged. (Regex now also matches `MemoriesRoutes.X`, resolved via reflection over the const table; doc unchanged.)
- [x] **Task 3 — Client generic send/decode** (AC: 4, 7)
  - [x] Introduce the generic `SendAsync<T>` helper. Standardize on the cancellation-aware "Variant B" catch filter (`OperationCanceledException when ct.IsCancellationRequested` rethrow, then `Exception when ex is JsonException or IOException or HttpRequestException or NotSupportedException`) and derive INVALID_RESPONSE messages from `typeof(T).Name`. (Implemented as private `SendAsync<T>` + `ReadRequiredAsync<T>`/`ReadOptionalAsync<T>`/`ThrowIfNotSuccessAsync`/`ToRemoteExceptionAsync`.)
  - [x] Migrate the 22 decode call sites to the helper. Keep the special shapes (`ExportCaseAsync`/`ExportTenantAsync` streaming with `ResponseHeadersRead`; `CreateTenantAsync`/`IngestAsync`/`StartConsistencyVerificationAsync`/`StartConsistencyRepairAsync` instance-id / `Uri` readers; `ProbeHealthAsync` bool+swallow; `GetTenantAsync`/`LookupMemoryUnitIdBySourceUriAsync`/`GetConsistencyVerificationStatusAsync`/`GetConsistencyRepairStatusAsync` `404 → null`; `ListTenantsAsync` pagination) as thin wrappers — do not flatten their behavior into the generic path.
  - [x] Keep all public methods `public virtual` and the class non-sealed (mockability guard). If `SendAsync<T>` is a new member, `protected`/`private` is fine. (All helpers are `private`; `MemoriesClientMockabilityContractTests` passes.)
- [x] **Task 4 — Client consumes the route table** (AC: 3)
  - [x] Replace the 23 method-grouping path literals (+ the 2 `relativePath` dups at L1088/L1214) with `MemoriesRoutes` builders. Preserve exact produced path + query strings (the tests assert them verbatim, e.g. `/api/tenants/acme/traverse` with `edgeTypes=causedBy%2CcorrelatedWith`). (No `"api/…"` literal remains in `MemoriesClient.cs`; guarded by a new test.)
- [x] **Task 5 — Fix `TraverseAsync` parameter order** (AC: 5) — ⚠️ **compat-overload disposition changed; see Completion Notes.**
  - [x] Add the corrected `TraverseAsync(tenantId, startNodeId, depth=2, caseId=null, edgeTypes=null, tokenBudget=null, CancellationToken ct=default)` (token last) and update its XML `<param>` order to match.
  - [x] ~~Keep the current positional signature as an `[Obsolete]` overload~~ — **NOT feasible**: two 7-parameter overloads differing only by swapping the trailing optional `ct`/`tokenBudget` compile to **CS0121 (ambiguous)** for ordinary calls like `TraverseAsync("acme","mu-1", ct: x)` (verified empirically). Per the maintainer decision (Jerome, and the story's own "accepted breaking reorder disposition"), the old order is **deleted** and the reorder ships as a **breaking change** — commit is `refactor(...)!` with a `BREAKING CHANGE:` footer (major bump). Only in-repo positional caller `TraverseRelationsTool.cs` was updated; named-argument callers (all tests) are source-compatible.
  - [x] Point internal callers and tests at the corrected overload. Re-run `MemoriesClientTraverseTests` (uses named args, so source-compatible) and grep for any positional `TraverseAsync(` caller in-repo before finalizing. (Updated the MCP caller `TraverseRelationsTool.cs` and the `StubMemoriesClient` override signature.)
- [x] **Task 6 — Verify** (AC: 7, 8)
  - [x] `dotnet build Hexalith.Memories.slnx` → 0 warnings. Run the client, server-route, MCP-stub, and mockability test filters listed in Dev Notes → Testing. `git diff --check` the touched paths. (Full solution Release build: 0 warnings / 0 errors; test suites green — see Completion Notes.)

### Review Findings

- [x] [Review][Patch] Formalize the approved breaking `TraverseAsync` contract — AC5/AC8 and the premise correction authorize the reordered signature and necessary MCP adaptation, but remediation commit `eb959d7` used `feat(tests):` rather than a breaking-change marker, so the required major-release signal remains open; published tag `v1.44.1` was not rewritten [_bmad-output/implementation-artifacts/25-3-shared-route-table-and-client-consolidation.md:27]
- [x] [Review][Defer] Remove the five unrelated submodule pointer advances from this no-submodule story [references/Hexalith.EventStore:1] — deferred, commit `8e92fe7` is published and later `main` commits depend on newer gitlinks; the user finalized the remediation without authorizing history rewriting or dependency rollback
- [x] [Review][Patch] Source emitted `Location` URIs and authorization route labels from `MemoriesRoutes` so the next route rename cannot leave them stale — already resolved by the later route-versioning work on current `main`; verified no endpoint `/api` literals remain and route-surface tests pass 11/11 [src/Hexalith.Memories.Server/Endpoints/CasesEndpoints.cs:77]
- [x] [Review][Patch] Reject whitespace and dot-segment route values before URI resolution can normalize a case export into a broader tenant export [src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs:334]
- [x] [Review][Patch] Cover the generic decoder's real empty body, cancellation, and `IOException`/`HttpRequestException`/`NotSupportedException` mappings — the tests exposed and the patch fixed early content buffering by using `ResponseHeadersRead` [tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientSendDecodeTests.cs:47]
- [x] [Review][Patch] Assert the exact request paths selected by the two handler client methods [tests/Hexalith.Memories.Cli.Tests/Cli/MemoriesClientHandlersContractTests.cs:73]

## Dev Notes

> The dev agent has ONLY this file. Read the whole Dev Notes section before editing. This is a behavior-preserving route/decode refactor except for the explicitly approved breaking `TraverseAsync` parameter reorder in AC5. The existing integration, contract, wire-surface, and drift-guard tests are the safety net — do not change what they assert; make them pass while removing duplication.

### ⚠️ Two premise corrections vs. the epic acceptance criteria (verified against code)

The epic text (`epics.md` §25.3) has two inaccuracies the dev must not take at face value:

1. **`TraverseAsync` is NOT `[Experimental]`.** `MemoriesClient.cs:940` doc comment reads *"Stable since Story 10.2."* There is no `[Experimental]` attribute. Reordering its public parameters is therefore a **breaking change to a published, packable API surface** (`Hexalith.Memories.Client.Rest` is `IsPackable=true`). The genuinely `[Experimental]` client methods are `CreateTenantAsync`, `CreateCaseAsync`, `GetTelemetrySummaryAsync` (`HXL001`) and `ListHandlersAsync`, `GetHandlerMismatchesAsync` (`HXL002`) — **not** `TraverseAsync`. The original compat-overload direction was superseded after the two swapped optional signatures proved CS0121-ambiguous. Jerome approved the breaking reorder during implementation, and Administrator reaffirmed that disposition during code review on 2026-07-11. AC5 and AC8 now encode the approved break and its narrowly required MCP adaptation; release metadata must signal a major change.
2. **"60 server literals" does not reconcile with the code.** Authoritative counts today: **46 server route registrations / 37 distinct path templates** in the 7 endpoint classes, **+6** non-mapping path literals in middleware/rate-limiter = **52** server-side path literals. Client side: **23 route-bearing method groupings / 20 distinct templates** (+2 duplicated `relativePath` literals). Do **not** chase the exact numbers "60/23"; the deliverable is *single-sourcing every route path literal*, and the tests (not a count) prove completeness.

### What this story is (and the audit finding it closes)

Closes **A21** (High, category R) from `research/architecture-audit-2026-07-04.md:52`:
> `Client.Rest/MemoriesClient.cs` (1,307 ln) vs `Program.cs` — Routes duplicated as 23 client + 60 server literals; **22× copy-pasted decode blocks with drifting catch-sets** → *silent runtime breakage on rename; inconsistent exception surface*. Fix: shared `MemoriesRoutes`; generic `SendAsync<T>`.

The **"drifting catch-sets"** is the subtle risk: the 22 decode blocks are **not identical**. There are two variants (see below). Consolidating to one `SendAsync<T>` is the goal, but it changes the exact catch behavior of the *Variant A* methods. That is acceptable **only** because it unifies toward the stricter, cancellation-aware *Variant B* behavior — but you must prove per-method that no wire-surface test regresses.

### Design decisions the dev must make (not left to guess)

- **Leading-slash reconciliation.** Server maps `"/api/…"` (leading slash, absolute); client uses `"api/…"` (relative, resolved against `HttpClient.BaseAddress`). Pick ONE canonical template form in `MemoriesRoutes` and adapt at each consumer (e.g., store templates *without* leading slash and have the server prepend `/`, or store *with* leading slash and have the client `TrimStart('/')`/use `UriKind.Relative`). Whatever you choose, the client's produced request path must stay `/api/…` (the `TestDelegatingHandler` tests assert the resolved path) and the server's mapped template must stay `/api/…`.
- **Parameter escaping.** The client currently interpolates segments through `Uri.EscapeDataString(...)` (e.g. `Uri.EscapeDataString(tenantId)`). Route-builder helpers on `MemoriesRoutes` must preserve that escaping so produced paths are unchanged. Query-string assembly (`BuildSearchPath`, traverse query) stays in the client — the table owns **path** templates, not query composition.
- **Two decode variants to unify** in `SendAsync<T>`:
  - *Variant A* (methods added earlier): inline `catch (System.Text.Json.JsonException) → new MemoriesRemoteException(..., "INVALID_RESPONSE", jsonException)`.
  - *Variant B* (methods added later): helper `CreateInvalidResponseException` + broader filter `catch (OperationCanceledException) when (ct.IsCancellationRequested) throw; catch (Exception ex) when (ex is JsonException or IOException or HttpRequestException or NotSupportedException)`.
  - Standardize on **Variant B** (cancellation-aware, more complete). The only per-call variation is the `INVALID_RESPONSE` message text `"...could not be parsed as {TypeName}"` — derive `{TypeName}` from `typeof(T).Name`.
- **`RouteSurfaceContractTests` extraction.** Today `MappedRouteRegex = app\.Map(Get|Post|Put|Delete|Patch)\(\s*"(/[^"]+)"` scans `Program.cs` + `*Endpoints.cs` for **inline literals**. After Task 2 the argument is a `MemoriesRoutes.X` reference, so the regex finds 0 routes and `ExtractMappedRoutes(...).Count.ShouldBeGreaterThan(0)` fails ("extraction regex or marker walk is broken"), and the count-tie fails. Update the extractor to resolve route constants — e.g., read `MemoriesRoutes.cs`, parse its `const string`/template members into a name→value map, and substitute when scanning the endpoint files; or reflect over `MemoriesRoutes` at test time. Keep the code→doc tie and the 46-route count-tie intact.

### Source tree — exact files to touch

**New:**
- `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs` — the route table (Task 1).

**Server (routes → table):**
- `src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs` (5 routes), `TenantLifecycleEndpoints.cs` (14), `CasesEndpoints.cs` (17), `ConsistencyEndpoints.cs` (5), `ExportEndpoints.cs` (2), `GraphEndpoints.cs` (2), `SearchEndpoints.cs` (1). All under `src/Hexalith.Memories.Server/Endpoints/`.
- `src/Hexalith.Memories.Server/Authentication/TenantAuthorizationMiddleware.cs` (`"/api/tenants"`, `"/api/search"`), `src/Hexalith.Memories.Server/Hosting/InboundRateLimitPartitionFactory.cs` (`"/api"`, `"/api/ingest"`, `"/api/ingest/url"`, `"/api/ingest/directory"`).
- `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs` — update extraction (Task 2).

**Client (client literals → table; 22× decode → `SendAsync<T>`; `TraverseAsync` reorder):**
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` (the whole story's client half — 1,354 lines; decode call sites at L69, 142, 179, 234, 282, 345, 402, 429, 478, 614, 811, 853, 899, 986, 1026, 1081, 1117, 1161, 1207, 1243, 1288, 1321; `TraverseAsync` at L941–948).

**Do NOT touch** (out of scope): CLI (`Hexalith.Memories.Cli`, Story 25.5), MCP tools (`Hexalith.Memories.Mcp`, Story 25.6), any contract rename / persistence split / `/api/v1/` versioning (Story 25.4), submodules under `references/`.

### Authoritative server route inventory (46 registrations / 37 templates)

The route table must reproduce each of these verbatim. Cross-check against `docs/operations/route-surface.md` (46 documented rows — the drift-guard count-tie).

- **Ingestion:** `POST /api/ingest`, `GET /api/ingest/{instanceId}`, `POST /api/ingest/url`, `POST /api/ingest/directory`, `GET /api/ingest/batches/{batchId}`
- **Search:** `GET /api/search`
- **Graph:** `GET /api/tenants/{tenantId}/traverse`, `PATCH /api/tenants/{tenantId}/edges/confidence`
- **Tenant lifecycle:** `GET|PUT /api/tenants/{tenantId}/embedding-config`, `POST /api/tenants`, `GET /api/tenants/{tenantId}/provision-status/{instanceId}`, `GET /api/tenants`, `GET|PATCH|DELETE /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/configuration`, `GET /api/tenants/{tenantId}/deletion-status/{instanceId}`, `POST /api/tenants/{tenantId}/verify`, `GET /api/tenants/{tenantId}/telemetry/summary`, `GET /api/handlers` *(HXL002)*, `GET /api/tenants/{tenantId}/handlers/mismatches` *(HXL002)*
- **Cases:** `POST|GET /api/tenants/{tenantId}/cases`, `GET|DELETE /api/tenants/{tenantId}/cases/{caseId}`, `GET .../cases/{caseId}/status`, `GET .../cases/{caseId}/failed-units`, `GET|DELETE .../cases/{caseId}/memory-units/{memoryUnitId}`, `GET .../cases/{caseId}/memory-units/by-source-uri`, `POST .../memory-units/{memoryUnitId}/re-ingest`, `POST .../failed-units/re-ingest`, `GET .../cases/{caseId}/activity`, `PUT|DELETE .../cases/{caseId}/members/{memberId}`, `GET .../cases/{caseId}/members`, `POST|GET .../memory-units/{memoryUnitId}/annotations`
- **Consistency:** `POST|GET /api/tenants/{tenantId}/consistency/verify`(+`/{instanceId}`), `GET .../consistency/inspect/{memoryUnitId}`, `POST|GET .../consistency/repair`(+`/{instanceId}`)
- **Export:** `GET /api/tenants/{tenantId}/cases/{caseId}/export`, `GET /api/tenants/{tenantId}/export`

Server wiring is `Program.cs:71–77` (`app.MapIngestionEndpoints()` … `app.MapGraphEndpoints()`), guarded by `RouteSurfaceContractTests.Program_InvokesAllDecomposedEndpointRegistrations`. There are **no `MapGroup` prefixes** — every literal is a full path.

### Cross-surface constraint: routes are also a Dapr ACL contract

`docs/operations/route-surface.md` publishes these routes as the ACL-verifiable operation surface for `accesscontrol.memories.yaml` (Dapr service invocation `method/api/…`). A route string change would silently break that ACL mapping. Because 25.3 keeps every string identical, the ACL stays valid — but this is exactly why **byte-identical** (AC 2/3) is non-negotiable.

### Architecture compliance (project-context.md — the load-bearing rules here)

- **Contracts stay implementation-neutral and versioned under `V1`.** `MemoriesRoutes` lives in `Contracts.V1`, exposes only framework-agnostic path strings, adds no Redis/FalkorDB naming, and does not depend on ASP.NET Core (keeps the `Client.Rest` package web-framework-free). Contracts dependency direction is `Contracts.V1 ← Server` and `Contracts.V1 ← Client.Rest` (both already reference Contracts — no csproj change needed).
- **Contract/JSON wire shape is a contract surface.** The route table is not serialized — keep it out of `MemoriesJsonContext`. This story must not alter any wire shape (Story 25.4 owns the persistence/contract split; do not pre-empt it).
- **C# conventions:** file-scoped namespace matching folder; ITANEO copyright header on every new `.cs`; `sealed`/`static` as appropriate; XML docs on all public members (warnings are errors); `ConfigureAwait(false)` in client/library code; central package management (never add `Version=` to a `.csproj`).
- **Tenant identifiers stay explicit** through client and server route parameters — do not collapse `{tenantId}` out of any template.
- **Commit as `refactor(...)`**, not `feat` — no new product capability. (Exception: if you adopt the *accepted breaking reorder* disposition for `TraverseAsync`, the commit/PR must carry a `BREAKING CHANGE:` footer for semantic-release.)

### Testing requirements

xUnit v3 + Shouldly + NSubstitute. Test folders mirror product areas. Do **not** weaken existing assertions — they are the behavior-preserving safety net.

- **Client wire surface (primary guard):** `tests/Hexalith.Memories.Cli.Tests/ClientRest/` — `MemoriesClientTests`, `MemoriesClientTraverseTests`, `MemoriesClientSearchTests`, `MemoriesClientLookupTests`, `MemoriesClientConsistencyTests`, `MemoriesClientExportTests`, `MemoriesAuthHandlerTests`, `MemoriesClientMockabilityContractTests`; plus `.../ClientRest/Cli/MemoriesClientWorkflowResponseTests`, `MemoriesClientHandlersContractTests`. These assert exact resolved path + query and decode/exception behavior via a `TestDelegatingHandler`. They must pass unchanged.
- **Server route drift:** `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs` — update the extractor (Task 2); all `[Fact]`s must still pass (`EveryMappedApiRoute_IsDocumented`, `DocumentedApiRowCount_EqualsMappedApiRouteCount`, `Program_InvokesAllDecomposedEndpointRegistrations`, `PubSubOperationSurface_IsTiedToCodeAndDocumented`, `HealthProbePaths_AreDocumentedFromConstants`, `NoProcessOperation_IsAbsentFromCodeAndRefutedInDoc`).
- **MCP stub:** `tests/Hexalith.Memories.Mcp.Tests/` uses `StubMemoriesClient : MemoriesClient` (subclass override). Keep all public methods `virtual`; the `MemoriesClientMockabilityContractTests` drift guard fails the build if the class is sealed or a public method drops `virtual`.
- **New coverage to add:** focused tests for `MemoriesRoutes` template/builder output (each template string equals the expected `/api/…`; builders escape segments); one test proving `SendAsync<T>` maps a non-2xx body → `MemoriesRemoteException` and a 2xx-empty/2xx-garbage body → `INVALID_RESPONSE` with the `typeof(T).Name`-derived message. Consider a guard test asserting no `"api/"`/`"/api/"` string literal survives in `MemoriesClient.cs` and the endpoint classes (drift prevention).
- **Suggested verification filters** (mirror Story 25.1/25.2 style):
  - `dotnet build Hexalith.Memories.slnx` → 0 warnings.
  - `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~ClientRest"`
  - `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~RouteSurfaceContractTests|FullyQualifiedName~DeploymentConfigurationContractTests"`
  - `dotnet test tests/Hexalith.Memories.Mcp.Tests/Hexalith.Memories.Mcp.Tests.csproj --filter "FullyQualifiedName~McpTool"`
  - Sandbox note: `dotnet test` may fail with `SocketException 13`; fall back to `dotnet exec` on the built xUnit v3 dll with `DiffEngine_Disabled=true` (see repo memory / CONTRIBUTING "Sandbox test runner workaround").

### Previous story intelligence (Epic 25 in-flight)

- **25.1 (done, `refactor(server): …`)** decomposed `Program.cs` into the 7 `*Endpoints.cs` classes and **explicitly deferred the shared route table and `/api/v1/` versioning to later stories** ("Do not try to make route groups change URLs in this story"). It also updated `RouteSurfaceContractTests` to scan endpoint files — you are now changing how those routes are expressed, so that test needs a second update.
- **25.2 (done, `refactor(server): centralize endpoint errors and telemetry`, commit `fc53166`)** added `ErrorResults`, tenant-id/tenant-status endpoint filters, `EndpointTelemetryFilter`, and `MemoriesServerExceptionHandler`, and added architecture **guard tests** (`EndpointCentralizationGuardTests`) that scan endpoint classes for duplicated patterns. When you edit the endpoint classes, do not disturb the 25.2 filters/telemetry wiring, and check whether the centralization guard needs the route-constant references allowlisted.
- **Review-hardening pattern from 25.1/25.2:** reviews repeatedly caught File-List/evidence gaps and drift-guard weaknesses. Keep the File List complete and prefer table-row-form / constant-tied assertions over bare `ShouldContain` substrings when you add guards.
- **Next stories depend on this one:** 25.4 versions these routes (`/api/v1/`) *through this same table*; 25.5 (CLI) and 25.6 (MCP) converge on it. The epic context is explicit: "coordinate the shared route table before CLI/REST client/MCP are updated broadly, otherwise consumers chase transient route names." So make `MemoriesRoutes` the clean seam 25.4 can version in one place.

### Git intelligence

Recent epic-25 commits: `fc53166 refactor(server): centralize endpoint errors and telemetry` (25.2), preceded by the 25.1 decomposition. Both are `refactor(<scope>)` — no `feat`. The spec/story markdown and `epic-25-context.md` are committed alongside the code. Current branch is `main`; create a feature branch **`story/25-3-shared-route-table-and-client-consolidation`** for the work — an earlier `bmad-dev-auto` run for 25.3 was blocked because the branch was still `story/25-2-…`, so name the branch to match this story key. Do not commit `bin/`/`obj/`.

### Latest tech notes

No new/changed dependencies. Everything uses the pinned stack (`net10.0`/C# 14, `System.Text.Json` source-gen via `MemoriesJsonContext`, `Microsoft.Extensions.Http`). The `[Experimental(...)]` mechanism is Roslyn's `System.Diagnostics.CodeAnalysis.ExperimentalAttribute` with diagnostic IDs `HXL001`/`HXL002` — relevant only insofar as `TraverseAsync` is **not** one of them (§ premise correction 1).

### Project Structure Notes

- New file location `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs` matches the flat `V1/`-only layout of the Contracts package (140 contracts under `V1/`, no `{Area}` sub-folders). Namespace `Hexalith.Memories.Contracts.V1`. No `Routing`/`Http`/`Api` sub-namespace exists today; do not invent one — keep it flat under `.V1`.
- Markdown artifacts in `_bmad-output/` are committed as **LF** (unlike C# which is CRLF). This story file is LF. New `.cs` files must be **CRLF** — after Write/Edit, run `sed -i 's/$/\r/'` on new/edited C# files.
- No detected structural conflicts. The only variance is the deliberate leading-slash normalization between server and client (documented above), which the route table resolves rather than perpetuates.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 25.3] — user story + AC (note the two premise corrections above).
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md:52] — A21 finding (23 client + 60 server literals; 22× decode; drifting catch-sets).
- [Source: _bmad-output/implementation-artifacts/epic-25-context.md] — epic goal, "shared route table single-source", route-versioning ordering, cross-story dependencies.
- [Source: _bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md] — endpoint decomposition; deferral of route table/versioning to later stories.
- [Source: _bmad-output/implementation-artifacts/spec-25-2-error-and-telemetry-centralization.md] — error/telemetry/tenant-filter centralization; endpoint-class guard tests.
- [Source: src/Hexalith.Memories.Server/Endpoints/*.cs, Program.cs:71–77] — 46 server route registrations; no `MapGroup`.
- [Source: src/Hexalith.Memories.Client.Rest/MemoriesClient.cs] — 23 client literals; 22× decode blocks (Variant A/B); `TraverseAsync` L941–948 (`ct`/`tokenBudget` transposition; "Stable since Story 10.2").
- [Source: src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs:49–55 & Endpoints/GraphEndpoints.cs:53–64] — canonical traverse parameter order (token last).
- [Source: src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs] — camelCase source-gen wire context (do not add the route table here).
- [Source: docs/operations/route-surface.md + tests/…/Deployment/RouteSurfaceContractTests.cs] — 46-route code↔doc drift guard + Dapr ACL surface.
- [Source: _bmad-output/project-context.md] — repo-wide C#/contract/testing/commit rules.

## Dev Agent Record

### Implementation Plan

- Preserve published history and add an append-only Conventional Commit carrying both a `!` marker and a `BREAKING CHANGE:` footer for the approved `TraverseAsync` parameter reorder; validate the new commit with the repository-pinned commitlint configuration before closing the review finding.

### Agent Model Used

Claude Opus 4.8 (1M context) — `claude-opus-4-8[1m]`.

### Debug Log References

- **Overload-ambiguity spike (Task 5):** a throwaway .NET 10 file-based app reproduced `error CS0121` for the two-overload `TraverseAsync` compat approach (`TraverseAsync("acme","mu-1", ct: x)` is ambiguous between the token-last and token-second-to-last 7-parameter overloads). This drove the AskUserQuestion that changed the disposition to an accepted breaking reorder.
- **Pre-existing baseline failure:** `IngestionWorkflowDeterminismGuardTests.DirectUrlIngestion_CapturesTraceContextBeforeDirectWorkflowSchedule` was already red at baseline `deb9dd79` — Story 25.1 moved the URL-ingestion endpoint out of `Program.cs` into `IngestionEndpoints.cs` but left the guard pointed at `Program.cs` (0 matches → `captureIndex == -1`). Repointed the guard at `IngestionEndpoints.cs` (the file this story consolidates); both search strings live there in the correct capture-before-schedule order.

### Completion Notes List

- **2026-07-11 breaking-release signal resolved:** Added append-only commit `30dab92` (`refactor(client)!: signal traversal API reorder`) with an explicit `BREAKING CHANGE:` footer, preserving published history while causing the next semantic release to recognize the approved `TraverseAsync` API break. Commitlint passes for the commit and the `v1.46.0..HEAD` range. Fresh validation: Release solution build 0 warnings/0 errors; Docker-free .NET inventory 4,283 passed / 1 environment-specific skip; Python tooling suites 104/104 passed; `git diff --check` clean.
- **2026-07-11 review remediation:** Administrator approved the breaking `TraverseAsync` reorder; AC5/AC8 and the premise correction now match that decision. Route builders reject whitespace and `.`/`..` segments, preventing `HttpClient` dot-segment normalization from widening a case-export request to tenant export. Generic GET decoding uses `ResponseHeadersRead`, allowing the documented `IOException`/`HttpRequestException`/`NotSupportedException` mapping to execute; focused tests now cover those failures, true empty content, cancellation, and exact handler routes. Current `main` already contains the absolute `MemoriesRoutes` location builders from later route-versioning work. Verification: Release solution build 0 warnings/0 errors; Contracts 579/579; CLI 445/445; route surface 11/11.
- **A21 closed.** HTTP routes are single-sourced in `MemoriesRoutes` (Contracts.V1): 37 template constants (leading-slash, `{placeholder}` tokens) consumed by the server, plus segment-escaping `*Path` builders consumed by the client. Contracts stays a pure library (no ASP.NET/routing package added; not registered in `MemoriesJsonContext`).
- **Server:** all 46 `app.MapX` registrations across the 7 `*Endpoints.cs` files now reference `MemoriesRoutes.*` (byte-identical templates); the 6 non-mapping literals in `TenantAuthorizationMiddleware`/`InboundRateLimitPartitionFactory` are sourced from the table. In-handler `Location`/response-header interpolations and auth-log/error-message strings were intentionally left as-is — AC 2 scopes to the 46 registrations + 6 middleware literals, and those other literals include human-readable error text that should not be forced through the route table.
- **Client:** the 22 copy-pasted decode blocks collapse to one generic `private SendAsync<T>` (GET → decode-required-`T`) backed by `ReadRequiredAsync<T>`/`ReadOptionalAsync<T>` (Variant-B cancellation-aware catch, `INVALID_RESPONSE` messages derived from `typeof(T).Name`) and `ThrowIfNotSuccessAsync`/`ToRemoteExceptionAsync`. Special shapes (streaming exports, instance-id/`Uri` readers, `ProbeHealthAsync`, `404 → null` reads, tenant pagination) keep bespoke behavior over the shared helpers. All `api/…` request paths derive from `MemoriesRoutes` builders — no path literal remains in `MemoriesClient.cs` (new guard test). Class stays non-sealed, public methods stay `virtual`, helpers are `private` — mockability guard passes.
- **Variant unification:** the earlier "Variant A" methods (JsonException-only catch) now use the stricter, cancellation-aware Variant B. No wire-surface test regressed (mid-body IO failures are not exercised; malformed/empty bodies map to `INVALID_RESPONSE` under both variants). `INVALID_RESPONSE` message text changed to the `typeof(T).Name`-derived form — no test asserts that text (only the `INVALID_RESPONSE` code).
- **⚠️ Task 5 disposition change (approved):** the story's "locked" compat-overload is infeasible (CS0121, proven). Jerome approved **Option B — reorder in place (breaking)**: corrected `TraverseAsync` has `CancellationToken ct` last; the old positional order is deleted. **This is a breaking change to the packable `Hexalith.Memories.Client.Rest` surface — the commit/PR MUST carry a `BREAKING CHANGE:` footer (or `refactor(...)!`) so semantic-release does a major bump.** Named-argument callers are unaffected; the one in-repo positional caller (`TraverseRelationsTool.cs`) and the `StubMemoriesClient` override were updated.
- **Route-surface drift guard:** `RouteSurfaceContractTests` now resolves `MemoriesRoutes.X` references by reflecting the const table (regex extended to match both inline literals and table references). `docs/operations/route-surface.md` is unchanged (verified) — the Dapr ACL surface is preserved.
- **Verification:** `dotnet build Hexalith.Memories.slnx -c Release` → **0 warnings, 0 errors**. Tests (sandbox `DiffEngine_Disabled=true dotnet exec` runner per CONTRIBUTING; VSTest TCP-listener limitation): Contracts.Tests **619/0**, Cli.Tests **424/0** (incl. all `ClientRest` wire-surface + mockability), Server.Tests **2509/0** (1 pre-existing environmental skip: `SubmoduleGuardTests`), Mcp.Tests **90/0**. `git diff --check` reports only `\r` on modified CRLF-committed lines (repo-wide LF/CRLF debt tracked in retros); no genuine trailing whitespace and no whole-file ending churn were introduced.

### File List

**New:**
- `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/MemoriesRoutesTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientSendDecodeTests.cs`

**Modified (server):**
- `src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs`
- `src/Hexalith.Memories.Server/Endpoints/TenantLifecycleEndpoints.cs`
- `src/Hexalith.Memories.Server/Endpoints/CasesEndpoints.cs`
- `src/Hexalith.Memories.Server/Endpoints/ConsistencyEndpoints.cs`
- `src/Hexalith.Memories.Server/Endpoints/ExportEndpoints.cs`
- `src/Hexalith.Memories.Server/Endpoints/GraphEndpoints.cs`
- `src/Hexalith.Memories.Server/Endpoints/SearchEndpoints.cs`
- `src/Hexalith.Memories.Server/Authentication/TenantAuthorizationMiddleware.cs`
- `src/Hexalith.Memories.Server/Hosting/InboundRateLimitPartitionFactory.cs`

**Modified (client + MCP consumer):**
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`
- `src/Hexalith.Memories.Mcp/Tools/TraverseRelationsTool.cs`

**Modified (tests):**
- `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Architecture/IngestionWorkflowDeterminismGuardTests.cs`
- `tests/Hexalith.Memories.Mcp.Tests/StubMemoriesClient.cs`

**Tracking:**
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (status → review)
- `_bmad-output/implementation-artifacts/25-3-shared-route-table-and-client-consolidation.md` (this file)

**Review remediation (2026-07-11):**
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`
- `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs`
- `tests/Hexalith.Memories.Cli.Tests/Cli/MemoriesClientHandlersContractTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientSendDecodeTests.cs`
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/ThrowingHttpContent.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/MemoriesRoutesTests.cs`

## Change Log

| Date | Change |
| ---- | ------ |
| 2026-07-11 | Closed the final review finding with append-only breaking-change commit `30dab92`, preserving published history while supplying the required major-release signal; commitlint, Release build, all 4,283 Docker-free .NET tests (1 environment-specific skip), and all 104 tooling tests passed. Story moved to review. |
| 2026-07-11 | Code-review remediation: approved and documented the breaking `TraverseAsync` disposition; rejected invalid/dot route segments; made generic GET decoding use `ResponseHeadersRead`; covered true empty content, cancellation, content-read failures, and exact handler routes; confirmed later route-versioning work already removed stale server locations. Historical submodule-pointer removal is deferred because `8e92fe7` is published and later `main` depends on newer gitlinks. Remediation commit `eb959d7` did not carry the required breaking marker, so the story remains in progress. |
| 2026-07-08 | Story 25.3 implemented: `MemoriesRoutes` single-source route table (Contracts.V1); 46 server registrations + 6 middleware literals + all client paths sourced from it; 22 client decode blocks consolidated behind generic `SendAsync<T>` (Variant-B, `typeof(T).Name` messages); `TraverseAsync` parameter order corrected (`ct` last) as an **accepted breaking reorder** (compat overload proven CS0121-ambiguous; requires `BREAKING CHANGE:` footer / major bump). Route-surface drift guard updated to resolve route constants; repointed a stale 25.1 ingestion-determinism guard. Added `MemoriesRoutes`/`SendAsync` coverage + a client no-literal guard. Full Release build 0/0; Contracts 619, Cli 424, Server 2509 (1 pre-existing skip), Mcp 90 — all green. |
