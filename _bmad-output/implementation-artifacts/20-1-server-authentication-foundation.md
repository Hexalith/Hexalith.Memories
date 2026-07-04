---
baseline_commit: 416882c80bd90a6421baa16efc8d62b148469bfc
---

# Story 20.1: Server Authentication Foundation

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want every server endpoint to require an authenticated principal,
so that no network caller can read, mutate, or destroy tenant data anonymously.

## Acceptance Criteria

1. Given JWT/OIDC bearer authentication is registered for the Memories Server with a fallback `RequireAuthenticatedUser` authorization policy, when any `/api/**` endpoint is called without a valid bearer token, then the request is rejected with HTTP 401 and a bearer challenge.

2. Given the Server still needs unauthenticated infrastructure probes and Dapr pub/sub wiring, when the fallback authorization policy is enabled, then only `/health`, `/alive`, `/ready`, `GET /dapr/subscribe`, and the Dapr pub/sub delivery route `POST /events/ingest` are explicitly anonymous.

3. Given the deferral comment at `src/Hexalith.Memories.Server/Program.cs:3122-3123` and the deferred-work entry `Story-9.3-MemoriesServerAuthN`, when this story is complete, then the code comment is removed or replaced with current evidence, and the deferred-work entry is resolved or cross-linked with evidence instead of duplicated.

4. Given MCP already has JWT bearer authentication and AppHost security propagation, when Server authentication is implemented, then the Server reuses the same configuration shape and validation invariants where practical without adding package versions to project files or introducing a new auth scheme.

5. Given Epic 20 is audit-remediation scope, when implementation and validation finish, then tests prove anonymous `/api/**` access fails, valid bearer access reaches at least one representative API endpoint, health and Dapr pub/sub endpoints remain reachable as intended, and no broad anonymous bypass exists.

## Tasks / Subtasks

- [x] Task 1 - Re-run the audit-anchor preflight before editing (AC: 1, 2, 3)
  - [x] Confirm the current auth gap with source search: `src/Hexalith.Memories.Server/Program.cs` has no `AddAuthentication`, `AddAuthorization`, `UseAuthentication`, `UseAuthorization`, `RequireAuthorization`, or `AllowAnonymous` wiring today.
  - [x] Confirm the deferral anchor still exists at `src/Hexalith.Memories.Server/Program.cs:3122-3123` and maps to `_bmad-output/implementation-artifacts/deferred-work.md` entry `Story-9.3-MemoriesServerAuthN`.
  - [x] Confirm Dapr pub/sub endpoint shape from `src/Hexalith.Memories.EventStore/EventIngestionController.cs`: controller route `events`, action route `ingest`, and `EnvironmentTopic(PubSubName, TopicEnvVar)`.
  - [x] Confirm Dapr actor handler behavior from `app.MapActorsHandlers()`. Do not silently add actors to the anonymous exception list; if Dapr actor runtime traffic requires an auth exception, document the finding, keep it Dapr-internal only, and add tests proving no `/api/**` route became anonymous.
  - [x] Record the preflight result in the Dev Agent Record with the date, current commit, any moved anchors, and how implementation adapted.

- [x] Task 2 - Add Server JWT bearer authentication options and registration (AC: 1, 4)
  - [x] Add Server-owned authentication option, configure, challenge-writer, and validator types under `src/Hexalith.Memories.Server/Authentication/`, following the existing MCP pattern in `src/Hexalith.Memories.Mcp/Authentication/`.
  - [x] Register `Authentication:JwtBearer` options with `.ValidateOnStart()`, an `IValidateOptions<T>` validator, and `IConfigureOptions<JwtBearerOptions>`.
  - [x] Use `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer()` and `builder.Services.AddAuthorization(...)` or `AddAuthorizationBuilder()` with a fallback policy built from `.RequireAuthenticatedUser()`.
  - [x] Add `Microsoft.AspNetCore.Authentication.JwtBearer` to `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` without a `Version` attribute; the central version is already supplied by `references/Hexalith.Builds/Props/Directory.Packages.props`.
  - [x] Preserve MCP as a separate surface; do not move MCP authentication classes into a shared project unless a small Server-owned helper is demonstrably needed for duplication reduction.

- [x] Task 3 - Wire authorization middleware and explicit anonymous exceptions (AC: 1, 2, 5)
  - [x] Insert `app.UseAuthentication()` and `app.UseAuthorization()` in the ASP.NET Core-recommended order so authentication/authorization run before protected endpoint delegates and controllers execute. Preserve existing CloudEvents middleware semantics for plain JSON and `application/cloudevents+json`; prove ordering with the existing EventStore middleware tests.
  - [x] Make `/health`, `/alive`, and `/ready` explicitly anonymous in `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` or at call sites. Do not weaken health-check response behavior or trace-exclusion behavior.
  - [x] Make `GET /dapr/subscribe` explicitly anonymous. If the Dapr ASP.NET extension route cannot be annotated directly, use the smallest route-specific convention or endpoint metadata approach that tests can prove.
  - [x] Make `POST /events/ingest` explicitly anonymous because it is the Dapr pub/sub delivery route. Do not mark all controllers anonymous; there is currently only this controller, but keep the exception specific.
  - [x] Audit `app.MapActorsHandlers()` under the fallback policy. If actor runtime endpoints need special handling, keep the exception separate from `/api/**`, name it explicitly in code/tests, and do not use a broad "all Dapr paths are anonymous" rule.
  - [x] Do not add `.AllowAnonymous()` to `/api/**`, `/api/handlers`, tenant, case, search, graph, consistency, export, ingestion, or telemetry endpoints.

- [x] Task 4 - Update AppHost/security propagation for the Server if needed (AC: 4)
  - [x] Check `src/Hexalith.Memories.AppHost/Program.cs` security wiring: MCP already receives `WithJwtBearerSecurity(security)` or propagated `Authentication__JwtBearer__*` environment variables.
  - [x] If the Server does not already receive equivalent JWT bearer configuration from `server.WithSecurityDependency(security)`, extend AppHost wiring and `AppHostSecurityConfigurationTests` so local/Aspire runs provide issuer, audience, authority/signing key, HTTPS metadata, and tenant claim name consistently.
  - [x] Keep Dapr API token propagation separate from bearer auth; do not replace Dapr sidecar token mode with JWT bearer authentication.

- [x] Task 5 - Resolve the existing deferred-work home (AC: 3)
  - [x] Update `_bmad-output/implementation-artifacts/deferred-work.md` entry `Story-9.3-MemoriesServerAuthN` to `resolved` with evidence, or add an explicit cross-link proving Story 20.1 owns the authentication foundation while Story 20.2 owns tenant authorization.
  - [x] Remove or replace the `AuthZ descoped per Spike 0.5` comment in `Program.cs` so it does not claim the Server remains globally anonymous after this story.
  - [x] Preserve the Epic 19 pattern: cross-link existing entries and avoid creating a second backlog home for the same risk.

- [x] Task 6 - Add focused auth tests and route drift guardrails (AC: 1, 2, 5)
  - [x] Add Server tests analogous to `tests/Hexalith.Memories.Mcp.Tests/Authentication/McpEndpointAllowAnonymousPathsTests.cs`: health and Dapr routes do not return 401 without a bearer token; representative `/api/**` route returns 401 without a bearer token.
  - [x] Add a route inventory guard that extracts mapped `/api/**` routes from `src/Hexalith.Memories.Server/Program.cs` and fails if any mapped API route has anonymous metadata or bypasses fallback auth.
  - [x] Add a valid-bearer test using a dev signing key that reaches at least one low-risk API endpoint far enough to avoid 401. Prefer an endpoint with substituted dependencies and no Redis/FalkorDB side effects.
  - [x] Add options/configuration tests for Server auth equivalent to MCP tests: missing Authority and SigningKey fails; weak SigningKey fails; OIDC mode sets Authority and RequireHttpsMetadata; strict token validation parameters are set.
  - [x] Preserve existing EventStore integration middleware tests for `/events/ingest` and `/dapr/subscribe`; update expected behavior only where auth changes require explicit anonymous assertions.

- [x] Task 7 - Validate and document completion (AC: 1, 2, 3, 4, 5)
  - [x] Run focused tests for Server auth and existing EventStore integration middleware.
  - [x] Run `dotnet build Hexalith.Memories.slnx` or document the exact blocker if the full build cannot run in this environment.
  - [x] Run `git diff --check -- src tests _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/20-1-server-authentication-foundation.md`.
  - [x] Update this story's Dev Agent Record with commands, outcomes, changed files, and any validation blockers.

## Dev Notes

This story is implementation scope, not a decision-only sweep. It closes audit finding A1: the Server HTTP API is currently globally anonymous. It does not implement tenant membership authorization, principal-derived audit identity, rate limiting, query escaping, or MCP production signing-key hardening. Those are Stories 20.2, 20.5, 20.6, and 20.4 respectively. [Source: _bmad-output/planning-artifacts/epics.md#Story-20.1; _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A1]

### Audit-Anchor Preflight

Re-verified on 2026-07-04 against the current worktree after `HEAD` `416882c80bd90a6421baa16efc8d62b148469bfc`:

- `src/Hexalith.Memories.Server/Program.cs` still has no Server auth wiring. Source search found no `AddAuthentication`, `AddAuthorization`, `UseAuthentication`, `UseAuthorization`, `RequireAuthorization`, or `AllowAnonymous` under `src/Hexalith.Memories.Server`. [Source: src/Hexalith.Memories.Server/Program.cs:40-357; local `rg` audit]
- The old 9.3 deferral is still present at `Program.cs:3122-3123`: handler endpoints were left unauthenticated because auth was descoped. [Source: src/Hexalith.Memories.Server/Program.cs:3122]
- Health endpoints are mapped in shared service defaults at `/health`, `/alive`, and `/ready`; they currently do not attach anonymous metadata because no fallback policy exists yet. [Source: src/Hexalith.Memories.ServiceDefaults/Extensions.cs:585-618; src/Hexalith.Memories.ServiceDefaults/Health/HealthEndpointPaths.cs:13-23]
- Dapr subscription discovery is mapped by `app.MapSubscribeHandler()` and the delivery route is `POST /events/ingest` through `EventIngestionController`. Both must remain reachable for Dapr pub/sub. [Source: src/Hexalith.Memories.Server/Program.cs:350-357; src/Hexalith.Memories.EventStore/EventIngestionController.cs:31-61]
- Dapr actor handlers are mapped by `app.MapActorsHandlers()` at `Program.cs:348`. The current source has no explicit auth metadata for actor endpoints because there is no fallback policy today. Treat this as an implementation preflight item: prove actor runtime traffic still works or record the narrow Dapr-internal exception with tests. [Source: src/Hexalith.Memories.Server/Program.cs:347-357]
- The route inventory remains mostly inline in `Program.cs`; Epic 25 owns decomposition. Do not use this story to refactor Program.cs broadly. [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A7]

If any anchor moves before dev starts, update this section first. Epics 20-26 explicitly require current-code re-verification before selection, creation, and implementation. [Source: _bmad-output/planning-artifacts/epics.md#Phase-Post-MVP-Audit-Remediation]

### Existing Auth Pattern to Reuse

The MCP host already implemented the local pattern to copy:

- `McpCompositionRoot` binds `Authentication:JwtBearer`, validates on start, registers `ConfigureJwtBearerOptions`, calls `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer().AddMcp(...)`, and calls `AddAuthorization()`. [Source: src/Hexalith.Memories.Mcp/McpCompositionRoot.cs:40-53]
- `ConfigureJwtBearerOptions` preserves inbound claim names, validates issuer, audience, signing key, lifetime, expiration, signed tokens, and allowed algorithms, supports OIDC authority or development signing key, and emits sanitized failure logs/challenges. [Source: src/Hexalith.Memories.Mcp/Authentication/ConfigureJwtBearerOptions.cs:31-105]
- `ValidateMcpAuthenticationOptions` fails startup when neither Authority nor SigningKey is configured, when issuer/audience/tenant claim are blank, when the dev key is under 32 effective bytes, or when valid algorithms are empty. [Source: src/Hexalith.Memories.Mcp/Authentication/ValidateMcpAuthenticationOptions.cs:20-52]
- `MemoriesMcpAuthenticationOptions` defines the expected config shape: `Authority`, `Audience`, `Issuer`, `SigningKey`, `RequireHttpsMetadata`, `TenantClaimName`, and `ValidAlgorithms`. [Source: src/Hexalith.Memories.Mcp/Authentication/MemoriesMcpAuthenticationOptions.cs:8-30]

For Server 20.1, reuse the invariants, not MCP-specific wording. Server challenge `realm`, problem detail type, and log category may differ. The key invariant is fail-closed default API access with sanitized 401 responses and no token logging.

### Architecture and Security Constraints

- Server auth should use JWT bearer. API callers send `Authorization: Bearer <token>`. The API validates the token and returns 401 for missing/invalid tokens, not redirects. [Source: Microsoft Learn, Configure JWT bearer authentication in ASP.NET Core, 2025-12-18]
- Required validation includes token signature, issuer, audience, and expiration; missing or incorrect critical claims should produce 401. [Source: Microsoft Learn, Configure JWT bearer authentication in ASP.NET Core, lines 116-129]
- The fallback policy is the intended default-deny mechanism: configure a policy with `RequireAuthenticatedUser()` and apply it as the fallback so endpoints without explicit authorization metadata require authentication. Explicit anonymous metadata is required for health and Dapr pub/sub exceptions. [Source: Microsoft Learn, Configure JWT bearer authentication in ASP.NET Core, lines 166-176; Microsoft Learn, authorization fallback policy, lines 1455-1465]
- Do not implement production symmetric-key hardening here. Story 20.4 owns the MCP HS256 production guard; if a Server dev signing-key validator is introduced here, it may allow symmetric keys for Development/Test but must not claim 20.4 is complete. [Source: _bmad-output/planning-artifacts/epics.md#Story-20.4]
- Do not derive tenant access or audit identity from the principal in this story. Story 20.2 owns claims-based tenant membership and replacing spoofable `x-user-id`. [Source: _bmad-output/planning-artifacts/epics.md#Story-20.2]

### File Structure Guidance

Expected production files:

- `src/Hexalith.Memories.Server/Authentication/MemoriesServerAuthenticationOptions.cs` (new)
- `src/Hexalith.Memories.Server/Authentication/ConfigureServerJwtBearerOptions.cs` or equivalent (new)
- `src/Hexalith.Memories.Server/Authentication/ValidateServerAuthenticationOptions.cs` or equivalent (new)
- `src/Hexalith.Memories.Server/Authentication/ServerProblemDetailsChallengeWriter.cs` or equivalent (new)
- `src/Hexalith.Memories.Server/Program.cs` (update auth registration, middleware, and stale deferral comment)
- `src/Hexalith.Memories.ServiceDefaults/Extensions.cs` (only if health endpoints need anonymous metadata at the source)
- `src/Hexalith.Memories.EventStore/EventIngestionController.cs` (only if `[AllowAnonymous]` is the narrowest way to keep Dapr pub/sub delivery reachable)
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` (add versionless JWT bearer package reference)
- `src/Hexalith.Memories.AppHost/Program.cs` and `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs` (only if Server bearer config propagation is missing)

Expected test files:

- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerAuthenticationOptionsTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/Authentication/ConfigureServerJwtBearerOptionsTests.cs` (new)
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs` (new)
- Existing EventStore integration tests may need narrow updates: `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/MiddlewareOrderTests.cs` and `EventStoreWebAppFactory.cs`.

Keep one primary C# type per file, keep ITANEO copyright headers in Memories-owned C# files, and use versionless package references. [Source: _bmad-output/project-context.md#Critical-Implementation-Rules]

### Testing Standards

Use xUnit v3, Shouldly, and NSubstitute. Avoid raw `Assert.*`. Test method names should be behavior-focused PascalCase. [Source: _bmad-output/project-context.md#Testing-Rules]

Minimum focused validation:

- Server auth options tests: missing Authority/SigningKey, weak signing key, blank issuer/audience, empty algorithms, OIDC mode, dev signing-key mode.
- JwtBearer configure tests: strict token validation parameters, `MapInboundClaims = false`, `ClockSkew = 1 minute`, `RequireExpirationTime = true`, `RequireSignedTokens = true`, allowed algorithms, OIDC Authority/RequireHttpsMetadata branch, symmetric SigningKey branch.
- Endpoint tests:
  - `GET /api/tenants` or another representative `/api/**` route without bearer returns 401 and `WWW-Authenticate: Bearer`.
  - At least one `/api/**` route with a valid dev bearer no longer returns 401.
  - `/health`, `/alive`, `/ready` without bearer do not return 401.
  - `GET /dapr/subscribe` without bearer returns the subscription payload, not 401.
  - `POST /events/ingest` without bearer still reaches the EventStore controller/middleware path.
  - Dapr actor handler posture is explicitly tested or documented. If actors remain protected, prove Dapr actor runtime calls work with auth enabled; if actors are anonymous, prove the exception is route-specific and does not cover `/api/**`.
  - A route-inventory guard prevents accidental anonymous `/api/**` bypass.

Run these likely commands, adjusting project paths if the implementation names differ:

```bash
dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~Authentication|FullyQualifiedName~EventStoreIntegration"
dotnet build Hexalith.Memories.slnx
git diff --check -- src tests _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/20-1-server-authentication-foundation.md
```

If the local test runner hits the known sandbox/VSTest limitation, use the built xUnit v3 executable pattern documented in prior stories and record the exact fallback command.

### Scope Boundaries

- Do not implement tenant membership authorization in this story. A valid bearer is enough for 20.1; tenant authorization is 20.2.
- Do not replace route literals, extract endpoint classes, or decompose `Program.cs` beyond what auth wiring requires. Epic 25 owns decomposition.
- Do not add API keys, cookies, Swagger auth flows, mTLS, PASETO, or custom HMAC URL signing.
- Do not log bearer tokens, raw JWT payloads, signing keys, client secrets, or full claim dumps.
- Do not make all non-`/api` endpoints anonymous by convention. The anonymous exceptions must stay named and test-proven.
- Do not treat Dapr actor handlers as implicitly allowed anonymous because they are Dapr-related. Actor runtime endpoints need their own preflight evidence and a narrow implementation decision.
- Do not change MCP behavior except for AppHost/shared security propagation if needed.
- Do not initialize or update nested submodules.

### Previous Story Intelligence

There is no previous Story 20.x file. Epic 19 close-out provides the handoff pattern:

- Cross-link `Story-9.3-MemoriesServerAuthN` and the D8 `TenantAuthorizationMiddleware` deferral instead of creating duplicate backlog homes.
- Check whether accepted/deferred entries are triggered before pulling them into implementation. For 20.1, `Story-9.3-MemoriesServerAuthN` is triggered by the 2026-07-04 security audit and this story should resolve or cross-link it. D8 is not fully resolved here because tenant authorization is Story 20.2.
- Parser validation alone is no longer sufficient. Epic 20 needs negative authentication and tenant-boundary tests.

[Source: _bmad-output/implementation-artifacts/epic-19-retro-2026-07-04.md#Next-Epic-Preparation; _bmad-output/implementation-artifacts/epic-19-retro-2026-07-04.md#Action-Items]

### Project Structure Notes

This story intentionally touches Server, EventStore integration, ServiceDefaults, AppHost, tests, and deferred-work governance. It should not touch `Hexalith.Memories.Mcp` except for read-only reference or if a small shared test helper is unavoidable. It should not change contracts, client REST APIs, CLI behavior, search logic, storage, or Web UI.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-20.1 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A1 - no Server authN/authZ and recommended fallback policy]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-20 - sequencing and cross-linking existing deferrals]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md#Story-9.3-MemoriesServerAuthN - active deferred auth home]
- [Source: src/Hexalith.Memories.Server/Program.cs - current Server composition root and route mappings]
- [Source: src/Hexalith.Memories.ServiceDefaults/Extensions.cs - health endpoint mapping]
- [Source: src/Hexalith.Memories.EventStore/EventIngestionController.cs - Dapr pub/sub delivery route]
- [Source: src/Hexalith.Memories.Mcp/McpCompositionRoot.cs and src/Hexalith.Memories.Mcp/Authentication/* - existing JWT bearer implementation pattern]
- [Source: docs/dev/adr-10.2-001-mcp-auth-shape-copy.md - auth shape invariants]
- [Source: docs/dev/adr-10.2-003-jwt-selection.md - JWT bearer selection rationale]
- [Source: docs/dev/mcp-server.md#Bearer-authentication - documented MCP bearer behavior]
- [Source: Microsoft Learn: Configure JWT bearer authentication in ASP.NET Core, https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0]
- [Source: Microsoft Learn: Create an ASP.NET Core app with user data protected by authorization, https://learn.microsoft.com/en-us/aspnet/core/security/authorization/secure-data?view=aspnetcore-10.0]
- [Source: _bmad-output/project-context.md - repo-wide C#, testing, package, and submodule rules]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-04 preflight at `416882c80bd90a6421baa16efc8d62b148469bfc`: `rg` found no Server `AddAuthentication`, `AddAuthorization`, `UseAuthentication`, `UseAuthorization`, `RequireAuthorization`, or `AllowAnonymous` wiring before implementation.
- 2026-07-04 preflight confirmed the stale `AuthZ descoped per Spike 0.5` comment at `src/Hexalith.Memories.Server/Program.cs:3123` and the active `_bmad-output/implementation-artifacts/deferred-work.md` entry `Story-9.3-MemoriesServerAuthN`.
- 2026-07-04 preflight confirmed Dapr pub/sub delivery is `POST /events/ingest` via `[Route("events")]`, `[HttpPost("ingest")]`, and `[EnvironmentTopic(PubSubName, TopicEnvVar)]`; actor runtime handlers are mapped separately with `app.MapActorsHandlers()` and are not folded into an `/api/**` anonymous bypass.
- 2026-07-04 validation: initial `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~Authentication|FullyQualifiedName~EventStoreIntegration|FullyQualifiedName~Deployment.AppHostSecurityConfigurationTests"` hit the known sandbox VSTest TCP listener failure (`SocketException 13`); used the documented xUnit v3 in-process runner instead.
- 2026-07-04 validation: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-07-04 validation: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ConfigureServerJwtBearerOptionsTests -class Hexalith.Memories.Server.Tests.Authentication.ServerAuthenticationOptionsTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.EventStoreIntegration.MiddlewareOrderTests -class Hexalith.Memories.Server.Tests.Deployment.AppHostSecurityConfigurationTests` passed 28/28.
- 2026-07-04 validation: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` passed 1969 total, 0 failed, 1 skipped.
- 2026-07-04 validation: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-07-04 validation: `git diff --check -- src tests _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/20-1-server-authentication-foundation.md` passed.
- 2026-07-04 review: Senior Developer Review found the Dapr actor anonymous exception was documented but not directly guarded by route inventory tests. Added `AnonymousRoutes_AreLimitedToNamedInfrastructureAndDaprActorRuntime`.
- 2026-07-04 review validation: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-07-04 review validation: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` passed 10/10.
- 2026-07-04 review validation: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ConfigureServerJwtBearerOptionsTests -class Hexalith.Memories.Server.Tests.Authentication.ServerAuthenticationOptionsTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.EventStoreIntegration.MiddlewareOrderTests -class Hexalith.Memories.Server.Tests.Deployment.AppHostSecurityConfigurationTests` passed 30/30.
- 2026-07-04 review validation: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-07-04 review validation: `git diff --check -- src tests _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/20-1-server-authentication-foundation.md _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/tests/test-summary-20-1-server-authentication-foundation.md` passed.

### Completion Notes List

- Create-story workflow completed an audit-anchor preflight against current code before drafting. No implementation code was changed by create-story.
- Story file includes the Epic 19 cross-link requirement for `Story-9.3-MemoriesServerAuthN` and keeps D8 tenant authorization for Story 20.2.
- Latest technical check used Microsoft Learn ASP.NET Core JWT bearer and fallback authorization guidance for .NET 10-era docs.
- Dev-story workflow started from baseline commit `416882c80bd90a6421baa16efc8d62b148469bfc`; implementation adapts by adding Server-owned JWT bearer auth, fallback authorization, named anonymous infrastructure routes, and route-drift tests.
- Implemented Server-owned JWT bearer options, startup validation, strict token validation configuration, and sanitized ProblemDetails bearer challenges using the existing MCP configuration shape and invariants.
- Added fallback authorization requiring authenticated users for Server endpoints, with explicit anonymous metadata for health probes, Dapr subscription discovery, Dapr pub/sub delivery, and Dapr actor runtime handlers; route inventory tests prove `/api/**` routes do not carry anonymous metadata.
- Updated AppHost so the Server receives `WithJwtBearerSecurity(security)` when Keycloak security is enabled, or propagated `Authentication__JwtBearer__*` environment values otherwise; Dapr API token propagation remains separate.
- Resolved deferred-work entry `Story-9.3-MemoriesServerAuthN` with Story 20.1 evidence while leaving tenant membership authorization and principal-derived audit identity to Story 20.2.
- Added Development-only bearer settings and test bearer helpers so in-process endpoint tests run against authenticated API requests without weakening Production configuration.
- Senior Developer Review added a route inventory guard proving anonymous routes are limited to named infrastructure, Dapr pub/sub discovery/delivery, and Dapr actor runtime endpoints; no `/api/**` route carries anonymous metadata.

### File List

- `_bmad-output/implementation-artifacts/20-1-server-authentication-foundation.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary-20-1-server-authentication-foundation.md`
- `src/Hexalith.Memories.AppHost/Program.cs`
- `src/Hexalith.Memories.EventStore/EventIngestionController.cs`
- `src/Hexalith.Memories.Server/Authentication/ConfigureServerJwtBearerOptions.cs`
- `src/Hexalith.Memories.Server/Authentication/MemoriesServerAuthenticationOptions.cs`
- `src/Hexalith.Memories.Server/Authentication/MemoriesServerProblemDetailsChallengeWriter.cs`
- `src/Hexalith.Memories.Server/Authentication/ValidateServerAuthenticationOptions.cs`
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj`
- `src/Hexalith.Memories.Server/Program.cs`
- `src/Hexalith.Memories.Server/appsettings.Development.json`
- `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/ConfigureServerJwtBearerOptionsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerAuthenticationOptionsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerTestBearerToken.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/ConsistencyEndpointTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/MiddlewareOrderTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/Infrastructure/TelemetryWebAppFactory.cs`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-07-04

Outcome: Approved after automatic fixes. No critical issues remain.

Findings fixed:

- [MEDIUM] `app.MapActorsHandlers().AllowAnonymous()` created a Dapr actor runtime anonymous exception, but the completed story only proved `/api/**` routes were not anonymous. Added `AnonymousRoutes_AreLimitedToNamedInfrastructureAndDaprActorRuntime` in `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs` so future drift fails if any route outside named infrastructure, Dapr pub/sub, or Dapr actor runtime becomes anonymous.
Validation:

- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests` - passed 10/10.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ConfigureServerJwtBearerOptionsTests -class Hexalith.Memories.Server.Tests.Authentication.ServerAuthenticationOptionsTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.EventStoreIntegration.MiddlewareOrderTests -class Hexalith.Memories.Server.Tests.Deployment.AppHostSecurityConfigurationTests` - passed 30/30.
- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `git diff --check -- src tests _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/20-1-server-authentication-foundation.md _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/tests/test-summary-20-1-server-authentication-foundation.md` - passed.

## Change Log

| Date | Phase | Summary |
|---|---|---|
| 2026-07-04 | create-story | Story drafted for Server JWT/OIDC bearer authentication foundation. Status -> ready-for-dev. |
| 2026-07-04 | dev-story | Implemented Server JWT bearer authentication foundation, fallback authorization, anonymous infrastructure exceptions, AppHost bearer propagation, deferred-work resolution, and focused auth/route guard tests. Status -> review. |
| 2026-07-04 | review | Senior Developer Review added a Dapr actor-runtime anonymous route guard and approved the story. Status -> done. |
