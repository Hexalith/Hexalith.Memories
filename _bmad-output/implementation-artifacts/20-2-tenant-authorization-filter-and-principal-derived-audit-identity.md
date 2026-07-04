---
baseline_commit: b48a519
---

# Story 20.2: Tenant Authorization Filter & Principal-Derived Audit Identity

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want tenant access enforced from authenticated principal claims and the audit user derived from the principal,
so that cross-tenant access is impossible and the FR67 audit trail is non-forgeable.

## Acceptance Criteria

1. Given the Memories Server has the Story 20.1 JWT bearer fallback authorization policy, when an authenticated caller requests any tenant-scoped Server API route with a `tenantId` that is not present in the caller's authorized tenant claims, then the request is rejected before endpoint business logic or backend access with HTTP 403 and a structured `TENANT_FORBIDDEN` error.

2. Given tenant membership is carried in JWT claims, when a token contains the configured `Authentication:JwtBearer:TenantClaimName` claim, a JSON/string-array `tenants` claim, or the existing alternate `tid`/`tenant` claims, then Server authorization normalizes those values into a request-scoped authorized tenant set using the same invariants as MCP tenant authorization.

3. Given a token contains duplicate tenant claims that differ only by claim-name casing and carry conflicting values, when authentication/authorization runs, then the principal is rejected fail-closed and no tenant-scoped endpoint executes.

4. Given read audit events currently call `ResolveReadOperationUser(...)` and use the spoofable `x-user-id` header, when any search, traverse, case-access, or other audited Server endpoint emits an `AccessTelemetryEvent`, then the user identity comes from the authenticated principal and `x-user-id` is ignored.

5. Given write/schedule audit scopes currently use caller-supplied identity fields such as `IngestedBy`, when ingest audit events are emitted after this story, then `EndpointTelemetryScope.User` is principal-derived; any remaining request-body identity fields are treated only as domain/content attribution and are not the audit identity.

6. Given Epic 20 is audit-remediation scope, when implementation finishes, then negative cross-tenant tests cover tenant path routes plus search axes (`syntactic`, `semantic`, `graph`, `hybrid`) and prove denial happens before tenant state, search, graph, actor, workflow, or registry dependencies are invoked. Closes A2.

## Tasks / Subtasks

- [x] Task 1 - Re-run the audit-anchor preflight before editing (AC: 1, 4, 6)
  - [x] Confirm current `src/Hexalith.Memories.Server/Program.cs` auth wiring from Story 20.1: `AddAuthentication`, `AddAuthorizationBuilder().SetFallbackPolicy(...)`, `UseAuthentication()`, `UseAuthorization()`, and the named anonymous infrastructure/Dapr exceptions still exist.
  - [x] Confirm the A2 anchor moved from the audit's stale `Program.cs:3245-3261` to `ResolveReadOperationUser(...)` at `src/Hexalith.Memories.Server/Program.cs:3263-3279`, where `x-user-id` is read.
  - [x] Confirm all tenant-carrying Server entry points still in scope: `/api/tenants/{tenantId}/**`, `/api/search?tenantId=...`, and ingest scheduling endpoints that carry tenant ID in the request body (`/api/ingest`, `/api/ingest/url`, `/api/ingest/directory`).
  - [x] Confirm tenantless status endpoints `GET /api/ingest/{instanceId}` and `GET /api/ingest/batches/{batchId}` remain Story 20.3 scope. Do not solve their instance-id tenant leak here.
  - [x] Record the preflight result in the Dev Agent Record with the date, current commit, moved anchors, and any implementation adaptation.

- [x] Task 2 - Add Server-owned principal normalization and tenant authorization services (AC: 1, 2, 3)
  - [x] Add Server-owned claims normalization under `src/Hexalith.Memories.Server/Authentication/`, using the MCP pattern from `MemoriesMcpClaimsTransformation` without making MCP internals public.
  - [x] Normalize configured `TenantClaimName`, `tenants` JSON/string-array claims, `tid`, and `tenant` into a Server-specific tenant claim type, for example `memories:tenant`.
  - [x] Add or register `IHttpContextAccessor` if required by the Server authorization helper.
  - [x] Add a request-scoped helper/accessor for the authorized tenant snapshot only if endpoint handlers need it; do not introduce global/static tenant state.
  - [x] Fail closed on unauthenticated principals, missing tenant claims, malformed tenant IDs, mismatched tenants, and conflicting case-insensitive tenant claim names.
  - [x] Keep security logs sanitized: log reason, route, source IP, and allowlisted claim names only; never log bearer tokens, signing keys, full JWT payloads, or unbounded claim dumps.

- [x] Task 3 - Apply tenant authorization before tenant-scoped business logic (AC: 1, 6)
  - [x] Implement a Server tenant authorization endpoint filter or small authorization helper that returns `Results.Json(new ErrorResponse("TENANT_FORBIDDEN", ...), statusCode: 403)` for cross-tenant access.
  - [x] Prefer a route group for `/api/tenants/{tenantId}` with the filter/metadata applied once, but keep the change scoped. Do not decompose `Program.cs` into endpoint classes; Epic 25 owns route factorization.
  - [x] If converting every tenant route to a `RouteGroupBuilder` is too risky in one pass, add the same filter to each `/api/tenants/{tenantId}/**` route and add a route-inventory test/metadata marker proving none are missed.
  - [x] Apply equivalent tenant authorization to `/api/search` because it carries `tenantId` in query string and is not under the tenant path group.
  - [x] Apply equivalent tenant authorization to ingest scheduling endpoints that carry tenant ID in the body before tenant guard, workflow scheduling, dedup reservation, actors, or search/index dependencies are touched.
  - [x] Leave `/api/handlers` as authenticated-only for this story unless an explicit operator-scope policy already exists; do not invent broad admin authorization.
  - [x] Leave `/api/tenants` collection list/create semantics unchanged unless a small, clearly tested principal-derived tenant guard is required for the specific implementation. Document any remaining collection-level admin posture as a follow-up risk, not a hidden completion claim.

- [x] Task 4 - Replace spoofable audit identity with principal-derived identity (AC: 4, 5)
  - [x] Replace `ResolveReadOperationUser(HttpContext, Activity?)` with a principal-derived resolver that ignores `x-user-id`.
  - [x] Prefer stable subject claims in this order unless implementation evidence shows a better local convention: `ClaimTypes.NameIdentifier`, `sub`, `preferred_username`, `name`; fall back to `AccessTelemetryLog.UserAnonymous` only when the authenticated principal has no usable identity claim.
  - [x] Preserve wizard-origin tagging only from a trusted principal claim or an explicit, documented authenticated claim. Do not keep `x-user-id=quickstart-wizard` as a magic audit path.
  - [x] Update search, traverse, case-access, ingest, URL ingest, and directory ingest `EndpointTelemetryScope.User` assignments to use the principal-derived resolver.
  - [x] Do not silently rewrite persisted content attribution fields (`IngestedBy`, `VerifiedBy`, annotation metadata) unless covered by existing contracts and tests. The mandatory deliverable is non-forgeable audit telemetry.

- [x] Task 5 - Update deferred-work governance (AC: 6)
  - [x] Update `_bmad-output/implementation-artifacts/deferred-work.md` to cross-link the old `TenantAuthorizationMiddleware` / D8 deferral to Story 20.2 evidence, following the Epic 19 pattern.
  - [x] Do not create a duplicate deferred entry for A2. Either resolve D8 if the story fully closes it, or leave an explicit residual only for out-of-scope admin/list/status endpoints with a concrete owner.
  - [x] Preserve `Story-9.3-MemoriesServerAuthN` as resolved by Story 20.1; do not reopen authentication foundation scope.

- [x] Task 6 - Add focused tests and drift guards (AC: 1, 2, 3, 4, 5, 6)
  - [x] Add claims transformation tests for configured `tenant_id`, `tenants` array/string values, `tid`, `tenant`, duplicate/conflicting case-insensitive tenant claims, and subject-to-nameidentifier behavior.
  - [x] Add tenant authorization tests for missing tenant claim, malformed requested tenant, mismatched tenant, matching tenant, and request-scoped tenant snapshot behavior.
  - [x] Add endpoint tests with `ServerTestBearerToken` extended to issue tokens for arbitrary subject/tenant sets.
  - [x] Add representative `/api/tenants/{tenantId}/**` cross-tenant denial tests that prove dependencies are not called. Good low-side-effect candidates: `/api/tenants/{tenantId}/handlers/mismatches`, `/api/tenants/{tenantId}/telemetry/summary`, and a case/memory-unit read route.
  - [x] Add `/api/search` negative tests for `axis=syntactic`, `axis=semantic`, `axis=graph`, and `axis=hybrid` that verify cross-tenant denial happens before search, graph, actor, or tenant status dependencies execute.
  - [x] Add at least one ingest scheduling denial test proving a token for tenant A cannot schedule body tenant B and no workflow/dedup dependency is invoked.
  - [x] Update `AuditLogStreamTests.SearchEndpoint_XUserIdHeader_EmitsHeaderValueAsAuditUser` into a regression test proving `x-user-id` is ignored and the audit user is principal-derived.
  - [x] Keep the Story 20.1 route guard `ApiRoutes_DoNotCarryAnonymousMetadata` and anonymous-route guard passing.

- [x] Task 7 - Validate and document completion (AC: 1-6)
  - [x] Run focused Server auth/tenant authorization/audit tests.
  - [x] Run the full Server test assembly with the xUnit v3 in-process runner if VSTest hits the known sandbox TCP listener failure.
  - [x] Run `dotnet build Hexalith.Memories.slnx` or document the exact blocker.
  - [x] Run `git diff --check -- src tests _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/20-2-tenant-authorization-filter-and-principal-derived-audit-identity.md`.
  - [x] Update this story's Dev Agent Record with commands, outcomes, changed files, and any validation blockers.

## Dev Notes

This story is implementation scope, not a decision-only sweep. It closes audit finding A2: tenant identity and audit user are currently caller asserted even after Story 20.1 added Server authentication. It builds on 20.1's bearer-token foundation; it must not rework authN, rate limiting, query escaping, MCP production signing-key hardening, status DTO projection, or route factorization. Those are Stories 20.1, 20.5, 20.6, 20.4, 20.3, and Epic 25 respectively. [Source: _bmad-output/planning-artifacts/epics.md#Story-20.2; _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A2]

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; key sections are Epic 20 and the post-MVP audit-remediation preflight.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; key sections identify `TenantAuthorizationMiddleware` as deferred D8 and require zero cross-tenant leakage.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; key requirements are FR44, FR67, and NFR8.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no UI work is in scope, but error states include unauthorized scope and tenant isolation failure.
- Loaded persistent facts from `_bmad-output/project-context.md`.
- Loaded previous story `_bmad-output/implementation-artifacts/20-1-server-authentication-foundation.md`.

### Audit-Anchor Preflight

Re-verified on 2026-07-04 against current `HEAD` `b48a519` plus the dirty working tree:

- Story 20.1 is done. Server now registers `MemoriesServerAuthenticationOptions`, `ValidateServerAuthenticationOptions`, `ConfigureServerJwtBearerOptions`, `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer()`, and a fallback authenticated-user policy. [Source: src/Hexalith.Memories.Server/Program.cs:47-57]
- Authentication middleware order is present: `UseAuthentication()` before `UseAuthorization()`. Anonymous exceptions are named for health, Dapr subscription/delivery, and Dapr actor runtime only. [Source: src/Hexalith.Memories.Server/Program.cs:361-375; tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs:91-131]
- The audit's `Program.cs:3245-3261` line anchor is stale after 20.1. Current spoofable read-audit identity is `ResolveReadOperationUser(...)`, which reads `httpContext.Request.Headers["x-user-id"]` and returns it as the audit user. [Source: src/Hexalith.Memories.Server/Program.cs:3263-3279]
- Write/schedule audit scopes currently use caller-supplied `IngestedBy` for `/api/ingest`, `/api/ingest/url`, and `/api/ingest/directory`. These are audit-scope assignments even if the request DTO field remains content attribution. [Source: src/Hexalith.Memories.Server/Program.cs:431,544,645]
- `/api/search` carries `tenantId` in query string and uses all search axes in one inline handler. It is outside the `/api/tenants/{tenantId}` path group but must be tenant-authorized for A2 to be closed. [Source: src/Hexalith.Memories.Server/Program.cs:2229-2979]
- `/api/tenants/{tenantId}/**` routes remain inline in `Program.cs` and include tenant configuration, provisioning/deletion status, consistency, export, cases, failed-unit re-ingestion, traverse, telemetry, handler mismatch, and confidence-promotion paths. [Source: src/Hexalith.Memories.Server/Program.cs:827-861,981-1250,1250-1813,1819-2227,2981-3239]
- `GET /api/ingest/{instanceId}` and `GET /api/ingest/batches/{batchId}` remain tenantless raw/status endpoints. Story 20.3 owns status scoping and DTO projection; this story should not claim those are fixed. [Source: src/Hexalith.Memories.Server/Program.cs:506-510,758-825; _bmad-output/planning-artifacts/epics.md#Story-20.3]

If any anchor moves before dev starts, update this section first. Epics 20-26 explicitly require current-code re-verification before selection, creation, and implementation. [Source: _bmad-output/planning-artifacts/epics.md#Phase-Post-MVP-Audit-Remediation]

### Existing Patterns to Reuse

- Server auth already has the configuration property `TenantClaimName = "tenant_id"` and validates it on startup. Reuse this option; do not add a parallel tenant-claim configuration key. [Source: src/Hexalith.Memories.Server/Authentication/MemoriesServerAuthenticationOptions.cs:23-30; src/Hexalith.Memories.Server/Authentication/ValidateServerAuthenticationOptions.cs:73-80]
- MCP already has a tenant-claim normalization and authorization pattern: `MemoriesMcpClaimsTransformation` normalizes configured `tenant_id`, `tenants`, `tid`, and `tenant`; `TenantClaimAuthorizationFilter` validates requested tenant IDs and snapshots the authorized tenant into `HttpContext.Items`; `AuthorizedTenantAccessor` reads the snapshot for downstream tools. Copy the invariants into Server-owned classes rather than exposing MCP internals. [Source: src/Hexalith.Memories.Mcp/Authentication/MemoriesMcpClaimsTransformation.cs:122-220; src/Hexalith.Memories.Mcp/Authentication/TenantClaimAuthorizationFilter.cs:16-107; src/Hexalith.Memories.Mcp/Authentication/AuthorizedTenantAccessor.cs:10-37]
- MCP tests already cover matching tenant, missing tenant claim, malformed tenant, mismatched tenant, stale request-scoped authorization state, and authorized-tenant accessor behavior. Mirror these behaviors for Server tests. [Source: tests/Hexalith.Memories.Mcp.Tests/TenantClaimAuthorizationTests.cs:22-116]
- Story 20.1 endpoint tests and `EventStoreWebAppFactory` provide a working in-process Server test fixture with development bearer settings and no real Redis/FalkorDB/Dapr requirement. Extend this fixture rather than creating a new integration harness. [Source: tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs:56-109; tests/Hexalith.Memories.Server.Tests/Authentication/ServerTestBearerToken.cs:14-47]

### Architecture and Security Constraints

- FR44 requires tenant context enforcement at all access layers with clear cross-tenant rejection. NFR8 is a hard gate for zero cross-tenant leakage. FR67 requires trustworthy per-tenant audit. [Source: _bmad-output/planning-artifacts/prd.md#FR44; _bmad-output/planning-artifacts/prd.md#FR67; _bmad-output/planning-artifacts/prd.md#NFR8]
- `TenantAuthorizationMiddleware` was deferred as D8 in architecture, but Epic 20 has now triggered it through A2. Implement the Server boundary guard here and cross-link the deferred register instead of creating another backlog home. [Source: _bmad-output/planning-artifacts/architecture.md#Decision-Log; _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Deferred-work-register]
- Tenant authorization must run before endpoint business logic touches tenant status, workflow clients, Dapr actors, registry services, search services, graph services, Redis/FalkorDB, or audit emission that would claim success.
- Use `ErrorResponse` for HTTP errors. Avoid ad hoc strings and avoid echoing malformed tenant input in logs or response text.
- Do not weaken the Story 20.1 fallback policy or anonymous route restrictions.
- Do not introduce package versions in `.csproj` files. No new package should be needed for this story.
- Keep one primary C# type per file with the ITANEO copyright header.

### Latest Technical Information

Microsoft's current ASP.NET Core Minimal API docs confirm route groups can apply common endpoint metadata and filters to grouped endpoints, including authorization, and filters can inspect handler arguments and `HttpContext` before invoking the endpoint delegate. This supports using a `RouteGroupBuilder` or endpoint filters for `/api/tenants/{tenantId}` authorization. [Source: Microsoft Learn, Route handlers in Minimal API apps, 2026-04-29; Microsoft Learn, Filters in Minimal API apps, 2026]

Microsoft's current claims authorization docs define claims as name/value pairs used during authorization and show policy/claim checks registered through `AddAuthorizationBuilder`, with `UseAuthorization()` after `UseAuthentication()`. Story 20.1 already established that middleware order; Story 20.2 should add tenant-claim evaluation, not new auth middleware ordering. [Source: Microsoft Learn, Claim-based authorization in ASP.NET Core, 2026]

### File Structure Guidance

Expected production files:

- `src/Hexalith.Memories.Server/Authentication/ServerTenantClaimsTransformation.cs` or equivalent (new)
- `src/Hexalith.Memories.Server/Authentication/TenantAuthorizationEndpointFilter.cs` or equivalent (new)
- `src/Hexalith.Memories.Server/Authentication/AuthorizedTenantAccessor.cs` or equivalent only if request-scoped tenant snapshots are consumed downstream (new)
- `src/Hexalith.Memories.Server/Authentication/MemoriesServerAuthenticationOptions.cs` (update only if a small allowlist/identity-claim option is justified)
- `src/Hexalith.Memories.Server/Program.cs` (update service registration, route/filter wiring, and audit identity resolver)
- `_bmad-output/implementation-artifacts/deferred-work.md` (cross-link/resolve D8 tenant authorization evidence)

Expected test files:

- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerTenantClaimsTransformationTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/Authentication/TenantAuthorizationEndpointFilterTests.cs` or equivalent (new)
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs` (update with cross-tenant endpoint coverage)
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerTestBearerToken.cs` (extend with subject and tenant parameters)
- `tests/Hexalith.Memories.Server.Tests/Telemetry/AuditLogStreamTests.cs` (update `x-user-id` regression)
- Existing endpoint tests may need narrow updates where valid bearer tokens now require matching tenant claims.

### Testing Standards

Use xUnit v3, Shouldly, and NSubstitute. Avoid raw `Assert.*`. Test names should be behavior-focused PascalCase. [Source: _bmad-output/project-context.md#Testing-Rules]

Minimum focused validation:

```bash
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ServerTenantClaimsTransformationTests -class Hexalith.Memories.Server.Tests.Authentication.TenantAuthorizationEndpointFilterTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.Telemetry.AuditLogStreamTests
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false
git diff --check -- src tests _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/20-2-tenant-authorization-filter-and-principal-derived-audit-identity.md
```

If `dotnet test` is preferred and the local test runner hits the known sandbox/VSTest TCP listener limitation, use the built xUnit v3 executable pattern documented in Story 20.1 and record the exact fallback command. [Source: _bmad-output/implementation-artifacts/20-1-server-authentication-foundation.md#Debug-Log-References]

### Scope Boundaries

- Do not reopen Story 20.1 authentication foundation. Keep fallback auth, challenge behavior, and anonymous route exceptions intact.
- Do not implement Story 20.3 workflow/batch status endpoint scoping or status DTO projection.
- Do not implement Story 20.4 MCP production signing-key hardening.
- Do not implement Story 20.5 rate limiting or audit completeness beyond principal-derived user identity for existing audit scopes touched here.
- Do not implement Story 20.6 RediSearch query injection hardening.
- Do not decompose `Program.cs` into endpoint classes, route-table abstractions, or shared route constants beyond what is required to apply the tenant filter. Epic 25 owns broader route factorization.
- Do not use `x-user-id` as trusted identity, including quickstart wizard tagging.
- Do not concatenate tenant/user input into graph queries.
- Do not initialize or update nested submodules.

### Previous Story Intelligence

Story 20.1 completed Server JWT bearer auth and established these important constraints:

- Server has strict JWT validation, sanitized challenges, fallback authorization, and AppHost bearer propagation. Build 20.2 on top of this instead of duplicating auth setup.
- Route drift is guarded: `/api/**` routes must not carry anonymous metadata, and anonymous routes are limited to named infrastructure/Dapr paths.
- The known local VSTest sandbox failure can be bypassed with xUnit v3 in-process execution.
- `ServerTestBearerToken` currently issues one token with `sub=operator-1` and `tenant_id=acme`; extend it for tenant/subject test cases instead of creating ad hoc JWT helpers.
- `Story-9.3-MemoriesServerAuthN` is resolved by 20.1. Tenant membership and principal-derived audit identity remain intentionally open for 20.2.

[Source: _bmad-output/implementation-artifacts/20-1-server-authentication-foundation.md#Completion-Notes-List; _bmad-output/implementation-artifacts/20-1-server-authentication-foundation.md#File-List]

### Git Intelligence

Recent commits show Epic 20 is now in implementation mode:

- `b48a519 feat(story-20.1): Server Authentication Foundation` added Server auth and route guardrails.
- `416882c Add orchestration state document, complexity data, and policy snapshot for Epic 20` and `c3c2dfe feat: add preflight complexity and snapshot files for Epics 20-26` added Epic 20-26 preflight/orchestration metadata.
- `4263ecb feat(audit): add audit-anchor preflight requirement for Epics 20-26 and validation check` confirms the audit-anchor preflight is mandatory for story files and implementation.

### Project Structure Notes

This story should touch Server auth/telemetry code, Server endpoint wiring, Server tests, and deferred-work governance. It should not change MCP production behavior except by read-only reference, and it should not change CLI, Web UI, client contracts, storage schemas, or search ranking logic except where a test fixture needs valid matching tenant claims.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-20.2 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A2 - caller-asserted tenant/user identity]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-20 - claims-based tenant filter and principal audit identity]
- [Source: _bmad-output/planning-artifacts/architecture.md#D8 - TenantAuthorizationMiddleware deferral]
- [Source: _bmad-output/planning-artifacts/prd.md#FR44-FR67-NFR8 - tenant enforcement, audit, zero leakage]
- [Source: _bmad-output/project-context.md - C#, testing, package, tenant isolation, and submodule rules]
- [Source: _bmad-output/implementation-artifacts/20-1-server-authentication-foundation.md - previous story implementation and review learnings]
- [Source: src/Hexalith.Memories.Server/Program.cs - current Server composition root, routes, and audit resolver]
- [Source: src/Hexalith.Memories.Server/Authentication/* - Story 20.1 Server auth foundation]
- [Source: src/Hexalith.Memories.Mcp/Authentication/* - existing tenant claim authorization pattern]
- [Source: tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs - Server route auth guard]
- [Source: tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs - in-process Server test fixture]
- [Source: Microsoft Learn: Route handlers in Minimal API apps, https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/route-handlers?view=aspnetcore-10.0]
- [Source: Microsoft Learn: Filters in Minimal API apps, https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/min-api-filters?view=aspnetcore-10.0]
- [Source: Microsoft Learn: Claim-based authorization in ASP.NET Core, https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims?view=aspnetcore-10.0]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story preflight on 2026-07-04 confirmed Story 20.1 is done and `20-2-tenant-authorization-filter-and-principal-derived-audit-identity` is the backlog story in `_bmad-output/implementation-artifacts/sprint-status.yaml`.
- Create-story preflight on 2026-07-04 confirmed A2's line anchors moved: `ResolveReadOperationUser(...)` currently reads `x-user-id` at `src/Hexalith.Memories.Server/Program.cs:3267`.
- Create-story preflight on 2026-07-04 confirmed `/api/search` and ingest scheduling endpoints carry tenant IDs outside the `/api/tenants/{tenantId}` path group and need explicit coverage for A2 closure.
- Dev-story preflight on 2026-07-04 re-confirmed Story 20.1 auth wiring in `Program.cs`, the stale A2 anchor at the previous `ResolveReadOperationUser(...)` helper, in-scope tenant path/search/body-ingest routes, and Story 20.3 ownership of tenantless ingest status endpoints. Baseline commit preserved as `b48a519`.
- Focused validation passed: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false`.
- Focused validation passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ServerTenantClaimsTransformationTests -class Hexalith.Memories.Server.Tests.Authentication.TenantAuthorizationEndpointFilterTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.Telemetry.AuditLogStreamTests`.
- Senior developer review on 2026-07-04 found and auto-fixed three issues: malformed nonblank tenant IDs bypassed authorization before endpoint execution; Server tenant syntax rejected underscores despite the MCP invariant allowing them; and `MemoryUnitLookupEndpoint` still used the spoofable `x-user-id` header for case-access audit identity.
- Review validation passed: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false`.
- Review validation passed: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ServerTenantClaimsTransformationTests -class Hexalith.Memories.Server.Tests.Authentication.TenantAuthorizationEndpointFilterTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.Telemetry.AuditLogStreamTests` -> 56 total, 0 failed.
- Full Server assembly validation passed after review fixes: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` -> 2000 total, 0 failed, 1 intentionally skipped submodule mutation guard.
- Solution build passed: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false`.
- Diff hygiene passed: `git diff --check -- src tests _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/20-2-tenant-authorization-filter-and-principal-derived-audit-identity.md _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/tests/test-summary.md _bmad-output/story-automator/orchestration-20-20260704-091304.md`.

### Completion Notes List

- Create-story workflow completed audit-anchor preflight against current code before drafting. No implementation code was changed by create-story.
- Story file includes the D8 tenant authorization cross-link requirement and preserves Story 20.3 ownership of tenantless workflow/batch status endpoints.
- Latest technical check used Microsoft Learn ASP.NET Core Minimal API route group/filter and claims authorization guidance for .NET 10-era docs.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added Server-owned principal tenant normalization with the normalized `memories:tenant` claim, subject-to-nameidentifier augmentation, JSON/string-array `tenants` support, configured `tenant_id`, `tid`, and `tenant` support, and fail-closed handling for conflicting case-insensitive claim-name variants.
- Added Server tenant authorization middleware/filter coverage for well-formed `/api/tenants/{tenantId}/**`, `/api/search?tenantId=...`, and body-tenant ingest scheduling requests. Cross-tenant denials return structured `TENANT_FORBIDDEN` before tenant status, actor, search, graph, workflow, Redis, or FalkorDB dependencies are invoked.
- Replaced spoofable audit user resolution with principal-derived identity for search, traverse, case access, ingest, URL ingest, and directory ingest. `x-user-id` and request-body attribution fields no longer supply audit identity.
- Updated deferred-work governance to resolve D8/A2 by Story 20.2 and explicitly leave tenantless ingest workflow/batch status scoping to Story 20.3.
- Updated Server test fixtures to mint matching tenant claims now that tenant-scoped Server routes enforce principal tenant membership.
- Review fix: tenant authorization now denies nonblank malformed path/query/body tenant values before endpoint business logic while preserving existing blank/missing request-shape validation.
- Review fix: Server tenant syntax now matches MCP tenant authorization by allowing underscores.
- Review fix: `MemoryUnitLookupEndpoint` now ignores `x-user-id` and derives case-access audit identity from the authenticated principal.

### File List

- `_bmad-output/implementation-artifacts/20-2-tenant-authorization-filter-and-principal-derived-audit-identity.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Memories.Server/Authentication/AuthorizedTenantAccessor.cs`
- `src/Hexalith.Memories.Server/Authentication/IAuthorizedTenantAccessor.cs`
- `src/Hexalith.Memories.Server/Authentication/ServerTenantClaimsTransformation.cs`
- `src/Hexalith.Memories.Server/Authentication/TenantAuthorizationEndpointFilter.cs`
- `src/Hexalith.Memories.Server/Authentication/TenantAuthorizationMiddleware.cs`
- `src/Hexalith.Memories.Server/Endpoints/MemoryUnitLookupEndpoint.cs`
- `src/Hexalith.Memories.Server/Program.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerTenantClaimsTransformationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerTestBearerToken.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/TenantAuthorizationEndpointFilterTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/ConsistencyEndpointTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/AuditLogStreamTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/Infrastructure/TelemetryWebAppFactory.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/TelemetrySummaryEndpointTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/TracePropagationNoDockerTests.cs`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-07-04.

Outcome: Approved after automatic fixes. No critical issues remain.

Findings fixed:

- HIGH: Malformed nonblank tenant values such as `bad~tenant` bypassed the new middleware/filter because authorization only ran after `IsWellFormedTenantId(...)` returned true. Fixed by invoking tenant authorization for nonblank tenant candidates and adding path/query/body denial tests proving no Dapr, actor, Redis, FalkorDB, workflow, or dedup dependencies are invoked.
- HIGH: `MemoryUnitLookupEndpoint` still derived case-access audit user from `x-user-id`, leaving an audited Server endpoint spoofable. Fixed by resolving audit user from `ClaimTypes.NameIdentifier`, `sub`, `preferred_username`, then `name`, and by adding a by-source-URI spoofing regression.
- MEDIUM: Server tenant syntax rejected `_` while MCP `TenantClaimAuthorizationFilter` permits underscores. Fixed the Server regex to match MCP and added an underscore invariant test.

Review notes:

- Git vs story file-list review found BMad orchestration/test-summary artifacts changed outside the original source/test file list; the source-impacting review fix added the missing `MemoryUnitLookupEndpoint` and affected telemetry endpoint tests to the File List.
- Microsoft Learn checks confirmed endpoint filters run before handlers and can inspect handler arguments, and route groups/filters are valid for common endpoint metadata/filtering. Claims authorization docs confirm multiple claim values and repeated claim types are valid authorization inputs.
- Story status set to `done` because all critical/high/medium review findings were auto-fixed and validation passed.

## Change Log

| Date | Phase | Summary |
|---|---|---|
| 2026-07-04 | create-story | Story drafted for Server tenant authorization and principal-derived audit identity. Status -> ready-for-dev. |
| 2026-07-04 | dev-story | Implemented Server tenant authorization and principal-derived audit identity; added focused normalization, authorization, endpoint denial, and audit regression tests. Status -> review. |
| 2026-07-04 | review | Senior developer review auto-fixed malformed tenant denial, MCP tenant regex parity, and extracted lookup audit identity. Status -> done. |
