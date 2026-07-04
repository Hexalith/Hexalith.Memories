---
baseline_commit: e444331
---

# Story 20.5: Inbound Rate Limiting, Quotas & Audit Completeness

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an operator,
I want per-tenant inbound rate limiting and complete audit emission,
so that one tenant cannot saturate the service and every mutating operation is audited.

## Acceptance Criteria

1. Given ASP.NET Core rate limiting is registered at the Memories Server request boundary, when authenticated API traffic reaches tenant-scoped endpoints, then requests are partitioned by authenticated tenant identity, not by spoofable body/query values or raw remote IP, and health plus Dapr infrastructure endpoints remain callable according to the existing anonymous-route policy.

2. Given a tenant exceeds its inbound request ceiling, when additional requests arrive during the active limiter window, then the server returns HTTP 429 with a structured `ErrorResponse`, emits low-cardinality telemetry tagged by tenant and error code, and does not execute endpoint business logic or downstream Dapr/Redis/FalkorDB dependencies.

3. Given tenant lifecycle and tenant configuration operations mutate tenant state, when tenant create, delete, provision status, deletion status, display-name update, and embedding-config update endpoints run, then each emits exactly one `AccessTelemetryEvent` with the principal-derived user, bounded content-free query params, outcome, error code where applicable, trace/span ids when available, and no raw bearer tokens, JWT payloads, provider credentials, request bodies, or cursor internals.

4. Given case membership, annotation, and deletion operations mutate case or memory-unit state, when case-member add/remove, annotation create, memory-unit delete, case delete, and tenant delete operations run, then each emits exactly one audit event on success and validation/error paths. The audit event must preserve `tenantId`, `caseId` when applicable, principal-derived user, operation type, bounded identifiers only, and must not rely on `CaseActivityService` events as the FR67 audit trail.

5. Given audit finding A41 also identified missing retention/TTL policy for access telemetry, when this story is completed, then either a bounded audit-retention configuration/purge mechanism is implemented and documented, or a named deferred-work entry is created with owner, trigger, and rationale. The story must not mark A41 fully closed while the retention slice is unaddressed.

6. Given Epic 20 is security remediation, when implementation finishes, then focused tests prove rate-limit partitioning and rejection behavior, audit emission coverage for the new mutating surfaces, principal-derived audit identity, sanitized 429/error payloads, metric tag policy, and no regression in existing authentication, tenant authorization, and audit-log stream tests.

## Tasks / Subtasks

- [x] Task 1 - Re-run audit-anchor preflight before editing (AC: 1-6)
  - [x] Confirm `src/Hexalith.Memories.Server/Program.cs` still has no `AddRateLimiter`, `UseRateLimiter`, `RequireRateLimiting`, `RateLimitPartition`, or inbound ASP.NET Core limiter wiring.
  - [x] Confirm existing `EmbeddingRateLimiterActor` and `PerTenantConcurrencyGate` are ingestion/provider controls only; do not reuse them as the ASP.NET request limiter.
  - [x] Confirm `AccessTelemetryLog` still exposes only `search`, `ingest`, `traverse`, `case-access`, and `delete` operation families with event IDs 7501-7505 and 7511-7515.
  - [x] Confirm `EndpointTelemetryScope` still dispatches only those operation families and only current ingest, search, traverse, and memory-unit lookup/read paths create telemetry scopes.
  - [x] Confirm tenant lifecycle, tenant configuration, case-member add/remove, annotation create, memory-unit delete, case delete, and tenant delete paths are still missing `EndpointTelemetryScope`.
  - [x] Record preflight result in the Dev Agent Record with current commit, moved anchors, and any implementation adaptation.

- [x] Task 2 - Add request-boundary per-tenant rate limiting (AC: 1, 2, 6)
  - [x] Add ASP.NET Core rate limiting services in `Program.cs` using `Microsoft.AspNetCore.RateLimiting` and `System.Threading.RateLimiting`.
  - [x] Partition route/query tenant endpoints by the authenticated, normalized tenant context established by Story 20.2. Prefer `AuthorizedTenantAccessor` after `TenantAuthorizationMiddleware` has set it; fall back to `__rejected__` only for unauthenticated or denied requests.
  - [x] Handle body-only tenant endpoints explicitly. `POST /api/ingest`, `/api/ingest/url`, and `/api/ingest/directory` expose `TenantId` only after body binding and the existing `TenantAuthorizationEndpointFilter`; ordinary rate-limiting middleware cannot safely see that tenant without buffering/parsing the body. Use one of these bounded approaches and test it: a tenant-aware endpoint filter that validates the bound body tenant and acquires a keyed limiter before scheduling, or additive tenant-routed aliases that let middleware partition by route tenant while preserving existing routes with a documented principal-level fallback. Do not partition these endpoints by raw body tenant before authorization.
  - [x] Handle tenant creation explicitly. `POST /api/tenants` has no existing authorized tenant route context because the target tenant may not exist yet; partition it by authenticated principal/admin identity or a dedicated tenant-provisioning partition, not by spoofable body `TenantId`.
  - [x] Configure limiter options from configuration, not literals only. Include sane defaults for development/test and production appsettings without adding package versions to `.csproj`.
  - [x] Use `QueueLimit = 0` unless an explicit product reason is documented; rate limiting should reject excess load rather than queue unbounded work.
  - [x] Call `app.UseRateLimiter()` in the correct middleware position after authentication/authorization and tenant authorization are available, and before endpoint business logic executes.
  - [x] Keep `app.MapDefaultEndpoints()`, Dapr actor handlers, and Dapr subscription endpoints aligned with existing anonymous-route tests. Do not rate-limit health probes in a way that breaks readiness/liveness.
  - [x] Add `OnRejected` handling that returns `ErrorResponse("RATE_LIMIT_EXCEEDED", ...)`, status 429, optional `Retry-After` when available, and sanitized logs/telemetry.

- [x] Task 3 - Add low-cardinality rate-limit telemetry (AC: 2, 6)
  - [x] Add a dedicated counter to `MemoriesMeter` such as `memories.rate_limit.rejections` with pinned tag keys `tenant_id` and `error_code`.
  - [x] Update `MemoriesMeter.MetricTagKeyPolicy` and `MemoriesMetricsTests` for the new instrument.
  - [x] Add a `TelemetryMetricsRecorder.RecordRateLimitRejection(...)` helper and tests proving it emits only pinned low-cardinality tags.
  - [x] Ensure rejected tenant or unauthenticated traffic uses `MemoriesMeter.RejectedTenantTag`; never tag by raw user, IP, token, path with IDs, request body, or exception message.

- [x] Task 4 - Extend audit operation taxonomy without breaking existing audit consumers (AC: 3, 4, 6)
  - [x] Add operation constants and source-generated logger methods for the missing mutating operation families. Suggested additive mapping inside the 7500-7599 bank: tenant-lifecycle 7506/7516, tenant-config 7507/7517, case-member 7508/7518, annotation 7509/7519, and keep delete 7505/7515 for tenant/case/memory-unit deletion unless a more precise delete taxonomy is required.
  - [x] Update `EndpointTelemetryScope` dispatch so each new operation type emits through its matching logger method and preserves current `search`, `ingest`, `traverse`, `case-access`, and `delete` behavior.
  - [x] Update `AccessTelemetryLogTests` and `EndpointTelemetryScopeTests` for all added operation constants/event IDs, success/error routing, partial/error semantics, trace/span propagation, and exactly-once dispose behavior.
  - [x] Keep `AccessTelemetryEvent` JSON shape additive-compatible; do not rename existing properties or operation constants.

- [x] Task 5 - Instrument tenant lifecycle and tenant configuration endpoints (AC: 3, 5, 6)
  - [x] Wrap `POST /api/tenants`, `GET /api/tenants/{tenantId}/provision-status/{instanceId}`, `PATCH /api/tenants/{tenantId}`, `PUT /api/tenants/{tenantId}/embedding-config`, `DELETE /api/tenants/{tenantId}`, and `GET /api/tenants/{tenantId}/deletion-status/{instanceId}` with telemetry scopes.
  - [x] Use `ResolvePrincipalAuditUser(httpContext, activity)` for the audit user. Do not use `x-user-id` or `operator@{remoteIp}` as the audit identity.
  - [x] Capture bounded query params only: tenant operation name, workflow instance id prefix/state where applicable, `forceReindex`, and field-count/changed-field names where safe. Do not serialize embedding provider secrets, base URLs with credentials, or full request bodies.
  - [x] Mark validation, tenant-status, Dapr-unavailable, conflict, and unhandled exception paths with correct outcome/error codes.
  - [x] Decide and document the A41 retention slice. If implementing now, keep it bounded to access telemetry retention config/purge evidence; if deferring, add a structured entry to `_bmad-output/implementation-artifacts/deferred-work.md` and reference it from this story.

- [x] Task 6 - Instrument case-member, annotation, and deletion operations (AC: 4, 6)
  - [x] Wrap `PUT /api/tenants/{tenantId}/cases/{caseId}/members/{memberId}` and `DELETE /api/tenants/{tenantId}/cases/{caseId}/members/{memberId}` with case-member audit scopes.
  - [x] Wrap `POST /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}/annotations` with an annotation audit scope.
  - [x] Wrap `DELETE /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` and `DELETE /api/tenants/{tenantId}/cases/{caseId}` with delete audit scopes.
  - [x] Ensure `CaseActivityService.RecordEventAsync(..., "system", ...)` remains a case activity stream only and is not treated as proof of FR67 audit emission.
  - [x] Preserve tenant authorization filtering from Story 20.2; audit scopes must not run business logic after a cross-tenant denial.

- [x] Task 7 - Add focused tests and validation (AC: 1-6)
  - [x] Add rate-limit tests under `tests/Hexalith.Memories.Server.Tests/Authentication`, `Endpoints`, or `Telemetry` using the existing TestServer factories. Prove a valid tenant can consume the configured limit, the next request returns 429 with `ErrorResponse`, and downstream substitutes receive no calls after rejection.
  - [x] Add a cross-tenant/claim test proving the limiter partitions by authenticated tenant, not spoofable tenant body/query values.
  - [x] Extend `AuditLogStreamTests` or add a sibling test file to drive validation-fail branches for tenant lifecycle/configuration, case-member, annotation, and deletion endpoints without requiring real Redis/FalkorDB/Dapr where possible.
  - [x] Add unit tests for any small helper extracted from `Program.cs`; keep Program decomposition limited because Story 25.1 owns broad `Program.cs` factorization.
  - [x] Run focused Server tests:

    ```bash
    DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false
    DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLogTests -class Hexalith.Memories.Server.Tests.Telemetry.EndpointTelemetryScopeTests -class Hexalith.Memories.Server.Tests.Telemetry.AuditLogStreamTests -class Hexalith.Memories.Server.Tests.Telemetry.TelemetryMetricsRecorderTests -class Hexalith.Memories.Server.Tests.Telemetry.MemoriesMetricsTests
    DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.Authentication.TenantAuthorizationEndpointFilterTests
    ```

  - [x] Run `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false` or document the exact blocker.
  - [x] Run `git diff --check -- src tests docs _bmad-output/implementation-artifacts/20-5-inbound-rate-limiting-quotas-and-audit-completeness.md _bmad-output/implementation-artifacts/sprint-status.yaml`.

## Dev Notes

This story closes the Epic 20 A41 security/operations slice by adding inbound request quotas at the ASP.NET Core boundary and completing FR67 audit event coverage for mutating operations. It must not be confused with existing embedding-provider throttling: `EmbeddingRateLimiterActor`, `RateLimiterLogic`, and `PerTenantConcurrencyGate` protect ingestion/provider work after requests are accepted; they do not bound inbound HTTP request load. [Source: _bmad-output/planning-artifacts/epics.md#Story-20.5; _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A41; src/Hexalith.Memories.Server/Program.cs:151-152; src/Hexalith.Memories.Server/Actors/EmbeddingRateLimiterActor.cs]

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; key section is Phase: Post-MVP Audit Remediation and Story 20.5 under Epic 20 API Security & Tenant Authorization.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant constraints are tenant isolation, API ingress security, per-tenant actor state, rate limiting and throttling, audit telemetry, low-cardinality metrics, Dapr workflow/actor boundaries, and warnings-as-errors.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements are FR42, FR44, FR45, FR67, FR69, NFR8, NFR11, and NFR22.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no UI implementation is in scope.
- Loaded persistent facts from `_bmad-output/project-context.md` and root-declared reference project-context files under `references/`.
- Loaded previous story `_bmad-output/implementation-artifacts/20-4-mcp-production-signing-key-hardening.md`.

### Audit-Anchor Preflight

Re-verified on 2026-07-04 against current `HEAD` `e444331`:

- A41 remains present in the audit evidence. It cites Server missing `AddRateLimiter`, no retention/TTL config, and incomplete `AccessTelemetryLog` emission sites. [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A41]
- `Program.cs` has authentication, fallback authorization, `TenantAuthorizationMiddleware`, Dapr infrastructure routes, and endpoint mappings, but no ASP.NET Core inbound rate-limiter registration or middleware. [Source: src/Hexalith.Memories.Server/Program.cs:38-55; src/Hexalith.Memories.Server/Program.cs:363-380]
- Existing rate-limit code is embedding/provider scoped: `EmbeddingRateLimiterActor` is registered as a Dapr actor and `GenerateEmbeddingActivity` consumes it before provider calls; `PerTenantConcurrencyGate` is registered for ingestion work. This is not inbound request limiting. [Source: src/Hexalith.Memories.Server/Program.cs:151-152; src/Hexalith.Memories.Server/Program.cs:331; src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:97-100]
- `AccessTelemetryLog` currently defines `search`, `ingest`, `traverse`, `case-access`, and `delete` operations only. Its logger event bank has success IDs 7501-7505 and error IDs 7511-7515. [Source: src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLog.cs]
- `EndpointTelemetryScope` currently switches only those five operation types. It emits audit records on dispose and records metrics through an optional callback, but new mutating operation families need dispatch support. [Source: src/Hexalith.Memories.Server/Telemetry/EndpointTelemetryScope.cs]
- Current audited endpoints include `/api/ingest` variants, `/api/search`, `/api/tenants/{tenantId}/traverse`, memory-unit read, and source-URI lookup. Tenant lifecycle/configuration, case-member add/remove, annotation create, memory-unit delete, case delete, and tenant delete do not create audit scopes today. [Source: src/Hexalith.Memories.Server/Program.cs; src/Hexalith.Memories.Server/Endpoints/MemoryUnitLookupEndpoint.cs]
- `CaseActivityService` records case activity events using `"system"` in several mutating case paths. That stream is useful product activity history but is not the principal-derived `AccessTelemetryEvent` audit trail required by FR67 and Story 20.2. [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs:89-92; src/Hexalith.Memories.Server/Cases/CaseService.cs:515-565; src/Hexalith.Memories.Server/Cases/CaseService.cs:685-686]

If any anchor moves before dev starts, update this section first. Epics 20-26 require current-code re-verification before implementation. [Source: _bmad-output/planning-artifacts/epics.md#Phase-Post-MVP-Audit-Remediation]

### Existing Patterns to Reuse

- Reuse `ResolvePrincipalAuditUser(HttpContext, Activity?)`; it already ignores spoofable `x-user-id` headers and derives the audit user from authenticated claims. [Source: src/Hexalith.Memories.Server/Program.cs:3348-3370]
- Reuse `EndpointTelemetryScope` for exactly-once audit emission, outcome tagging, error code tagging, trace/span propagation, and metric callback protection. Extend it rather than creating parallel audit emitters. [Source: src/Hexalith.Memories.Server/Telemetry/EndpointTelemetryScope.cs; tests/Hexalith.Memories.Server.Tests/Telemetry/EndpointTelemetryScopeTests.cs]
- Reuse `ErrorResponse` for structured 429 and audit-path errors. Do not introduce ad hoc anonymous error shapes for rate-limit rejections. [Source: _bmad-output/project-context.md#Critical-Implementation-Rules]
- Reuse the TestServer factories: `EventStoreWebAppFactory` for auth/tenant authorization route behavior and `TelemetryWebAppFactory` plus `CapturingAuditLoggerProvider` for audit-log stream assertions without real Redis/FalkorDB/Dapr. [Source: tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs; tests/Hexalith.Memories.Server.Tests/Telemetry/Infrastructure/TelemetryWebAppFactory.cs]
- Reuse `MemoriesMeter.MetricTagKeyPolicy` and `TelemetryMetricsRecorder` for new metrics so tag-key drift is caught by existing tests. [Source: src/Hexalith.Memories.Telemetry/MemoriesMeter.cs; src/Hexalith.Memories.Server/Telemetry/TelemetryMetricsRecorder.cs; tests/Hexalith.Memories.Server.Tests/Telemetry/MemoriesMetricsTests.cs]

### Architecture and Security Constraints

- Tenant ID must remain explicit through API, workflow, storage, telemetry, and audit paths. Rate-limit partition keys must come from authenticated/authorized tenant context, not caller-supplied values alone. [Source: _bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- Middleware ordering matters for partition correctness. `TenantAuthorizationMiddleware` sets `AuthorizedTenantAccessor.HttpContextItemKey` only for tenant path/query routes before endpoint execution; `TenantAuthorizationEndpointFilter` resolves body-bound ingest tenants later. Do not build a global limiter that assumes every endpoint has an authorized tenant item before routing to the endpoint. [Source: src/Hexalith.Memories.Server/Authentication/TenantAuthorizationMiddleware.cs; src/Hexalith.Memories.Server/Authentication/TenantAuthorizationEndpointFilter.cs; src/Hexalith.Memories.Server/Authentication/AuthorizedTenantAccessor.cs]
- Keep unauthenticated API behavior from Story 20.1: `/api/**` requires bearer auth; health and Dapr infrastructure routes are the existing anonymous exceptions. Rate limiter placement must not weaken fallback authorization or `TenantAuthorizationMiddleware`. [Source: _bmad-output/implementation-artifacts/20-1-server-authentication-foundation.md; src/Hexalith.Memories.Server/Program.cs:363-380]
- Keep tenant authorization fail-closed from Story 20.2. Cross-tenant requests must be forbidden before endpoint dependencies are touched; rate limiting must not mask or bypass tenant authorization. [Source: _bmad-output/implementation-artifacts/20-2-tenant-authorization-filter-and-principal-derived-audit-identity.md; tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs]
- Audit query params must stay bounded and content-free. Never log content bodies, authorization headers, raw tokens, JWT payloads, secrets, provider credentials, cursor internals, full embedding config objects, or exception messages as metric labels. [Source: tests/Hexalith.Memories.Server.Tests/Telemetry/AuditLogStreamTests.cs; _bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- Avoid broad `Program.cs` decomposition in this story. A tiny helper or focused telemetry endpoint wrapper is acceptable if it reduces copy-paste risk, but Story 25.1 owns full Program.cs factorization. [Source: _bmad-output/planning-artifacts/epics.md#Story-25.1]
- Do not initialize or update nested submodules.

### Latest Technical Notes

- Official ASP.NET Core guidance for .NET 10 uses `builder.Services.AddRateLimiter(...)`, optional `options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(...)`, and `app.UseRateLimiter()`. Endpoint-specific APIs require `UseRateLimiter` after routing; global limiters can run earlier, but this story needs authenticated tenant context, so middleware ordering must be chosen deliberately. [Source: https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0#use-rate-limiting-middleware]
- The same guidance describes partitioned rate limiting as separate buckets keyed by user, IP, API key, or another stable key. For this story, the stable key is the authenticated tenant, not user identity or IP. [Source: https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0#rate-limiting-partitions]
- Microsoft explicitly recommends load/stress testing rate-limiter options before production and warns that user-input-derived partitions can create DoS risk. Keep partition keys low-cardinality and trusted. [Source: https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0#testing-endpoints-with-rate-limiting]

### Expected File Touches

Likely production files:

- `src/Hexalith.Memories.Server/Program.cs` - add rate limiter registration/middleware and telemetry scopes around the targeted endpoints.
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLog.cs` - add operation constants and logger methods.
- `src/Hexalith.Memories.Server/Telemetry/EndpointTelemetryScope.cs` - dispatch new operation families.
- `src/Hexalith.Memories.Server/Telemetry/TelemetryMetricsRecorder.cs` - add rate-limit rejection recorder.
- `src/Hexalith.Memories.Telemetry/MemoriesMeter.cs` - add rate-limit metric and tag-key policy.
- `src/Hexalith.Memories.Server/appsettings.json`, `appsettings.Development.json`, and/or `appsettings.Production.json` - add rate-limit defaults and optional retention config.
- `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs` - update only if tenant display-name handler instrumentation cannot remain in `Program.cs`.
- `_bmad-output/implementation-artifacts/deferred-work.md` - update only if the A41 retention/TTL slice is explicitly deferred rather than implemented.
- `docs/operations/*` or `docs/dev/*` - update only for rate-limit and retention operator-facing configuration.

Likely test files:

- `tests/Hexalith.Memories.Server.Tests/Telemetry/AccessTelemetryLogTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/EndpointTelemetryScopeTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/AuditLogStreamTests.cs` or a new sibling audit coverage test.
- `tests/Hexalith.Memories.Server.Tests/Telemetry/TelemetryMetricsRecorderTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/MemoriesMetricsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs` or a focused rate-limit endpoint test.
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs` or `TelemetryWebAppFactory.cs` if test configuration must lower limiter ceilings.

### Scope Boundaries

- Do not change MCP auth/signing-key behavior; Story 20.4 is complete.
- Do not implement RediSearch escaping; Story 20.6 owns A31.
- Do not use the embedding `RateLimiterLogic` as the HTTP limiter; that actor is durable provider throttle state.
- Do not read, buffer, or parse request bodies inside a global rate-limiter partition function unless the implementation also proves body-size safety, JSON failure behavior, and no duplicate-read regression. Prefer an endpoint filter or route-tenant partition for body-bound tenant requests.
- Do not introduce Redis-backed distributed rate limiting unless the implementation proves in-process ASP.NET Core limiting is insufficient for the accepted deployment shape. A distributed limiter would be a larger architecture decision.
- Do not turn audit logging into a tamper-evident certified audit store. PRD states access telemetry is infrastructure telemetry; certified audit trails remain an operator/application responsibility. [Source: _bmad-output/planning-artifacts/prd.md#Non-Goals-And-Explicit-Exclusions]
- Do not add high-cardinality metric labels such as `case_id`, `user`, `memory_unit_id`, request path with IDs, source URI, or token values.

### Previous Story Intelligence

Story 20.4 completed MCP production signing-key hardening and reinforced these Epic 20 patterns:

- Keep remediation stories narrow and evidence-driven. Do not pull adjacent Epic 20/25 work into this story unless required by A41.
- Security invariants should fail before sensitive downstream work executes. For 20.5, 429 rejection should happen before endpoint business logic and backend calls.
- Preserve sanitized errors. Tests in 20.4 explicitly checked that challenge/validation output did not leak signing keys; 20.5 should apply the same discipline to tokens, provider config, and request bodies.
- Use focused tests plus the `dotnet exec` xUnit v3 fallback when VSTest is blocked by sandbox TCP-listener limits.
- Keep story File List complete; the 20.4 review found omissions for a new test file and test summary artifact.

[Source: _bmad-output/implementation-artifacts/20-4-mcp-production-signing-key-hardening.md#Previous-Story-Intelligence; git commit `e444331`]

### Git Intelligence

Recent commits show Epic 20 is in active security remediation:

- `e444331 feat(story-20.4): MCP Production Signing-Key Hardening` added production MCP auth validation, startup wiring tests, and sanitized docs.
- `ef57bd5 feat(story-20.3): Tenant-Scope Workflow & Batch Status Endpoints` added safe status projection, tenant-first status authorization, and endpoint tests.
- `ae9558f feat(story-20.2): Tenant Authorization Filter & Principal-Derived Audit Identity` added normalized tenant claims, endpoint filters/middleware, and cross-tenant denial tests.
- `b48a519 feat(story-20.1): Server Authentication Foundation` added bearer authentication, fallback authorization, anonymous route guardrails, and Server auth tests.

### Testing Standards

Use xUnit v3, Shouldly, and NSubstitute. Avoid raw `Assert.*`. Test names should be behavior-focused PascalCase, and test folders should mirror product areas. [Source: _bmad-output/project-context.md#Testing-Rules]

For TestServer tests, keep `DiffEngine_Disabled=true` for xUnit executable runs. If `dotnet test` hits the known sandbox/VSTest TCP-listener limitation, use the built test assembly with `dotnet exec` and record the exact command and outcome. [Source: CONTRIBUTING.md#Sandbox-test-runner-workaround]

### Project Structure Notes

This story is primarily Server and Telemetry work. Keep all C# files one type per file if adding new helpers or options. Preserve ITANEO copyright headers for new Memories-owned `.cs` files. Do not add package versions to `.csproj`; central package management and the ASP.NET Core shared framework should cover the rate-limiting namespace unless a focused compile proves otherwise.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-20.5 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A41 - rate limiting, retention, audit completeness gap]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-20 - approved remediation scope]
- [Source: _bmad-output/planning-artifacts/prd.md#FR42-FR45-FR67-FR69-NFR8-NFR11-NFR22 - tenant config, audit, rate limit, auth requirements]
- [Source: _bmad-output/planning-artifacts/architecture.md#Rate-Limiting-and-Throttling - existing provider throttle context]
- [Source: _bmad-output/project-context.md - C#, testing, package, telemetry, tenant isolation, and secrets rules]
- [Source: _bmad-output/implementation-artifacts/20-4-mcp-production-signing-key-hardening.md - previous story implementation and validation learnings]
- [Source: src/Hexalith.Memories.Server/Program.cs - current endpoint mappings and missing inbound rate limiter]
- [Source: src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLog.cs - current audit operation/event-id bank]
- [Source: src/Hexalith.Memories.Server/Telemetry/EndpointTelemetryScope.cs - current audit scope dispatch]
- [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs - case activity stream, not FR67 audit trail]
- [Source: tests/Hexalith.Memories.Server.Tests/Telemetry/AuditLogStreamTests.cs - current audit stream pattern and credential-key guard]
- [Source: tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs - auth/endpoint TestServer fixture]
- [Source: tests/Hexalith.Memories.Server.Tests/Telemetry/Infrastructure/TelemetryWebAppFactory.cs - audit TestServer fixture]
- [Source: https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0 - ASP.NET Core rate limiting middleware]

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- 2026-07-04: Preflight at `e444331b4aa1f616570e1feab925f9f3c81dd2c3` confirmed no inbound ASP.NET Core limiter, ingestion/provider-only throttles, original five audit operation families, and missing mutation audit scopes; no anchor moves required.
- 2026-07-04: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false` passed.
- 2026-07-04: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Telemetry.AccessTelemetryLogTests -class Hexalith.Memories.Server.Tests.Telemetry.EndpointTelemetryScopeTests -class Hexalith.Memories.Server.Tests.Telemetry.AuditLogStreamTests -class Hexalith.Memories.Server.Tests.Telemetry.MutationAuditLogStreamTests -class Hexalith.Memories.Server.Tests.Telemetry.TelemetryMetricsRecorderTests -class Hexalith.Memories.Server.Tests.Telemetry.MemoriesMetricsTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests -class Hexalith.Memories.Server.Tests.Authentication.TenantAuthorizationEndpointFilterTests -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointRateLimitTests` passed: 133 tests, 0 failed.
- 2026-07-04: BMAD QA generate E2E tests workflow added missing TestServer API coverage for rate-limit infrastructure exemption/principal fallback and mutation audit coverage for tenant delete/config/status, case-member remove, and case delete paths.
- 2026-07-04: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build Hexalith.Memories.slnx --disable-build-servers -m:1 /nr:false` passed.
- 2026-07-04: `git diff --check -- src tests docs _bmad-output/implementation-artifacts/20-5-inbound-rate-limiting-quotas-and-audit-completeness.md _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/tests/test-summary.md` passed.
- 2026-07-04: Story-automator review auto-fixed two findings, rebuilt `Hexalith.Memories.Server.Tests`, and reran the focused xUnit class set: 133 tests, 0 failed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added ASP.NET Core inbound rate limiting with configuration-bound fixed-window defaults, zero queueing, sanitized 429 `ErrorResponse`, and low-cardinality rejection metrics.
- Partitioned route/query API traffic by authorized tenant context after `TenantAuthorizationMiddleware`; body-bound ingest routes use a tenant-aware endpoint filter after body binding and tenant authorization; tenant creation falls back to authenticated principal partitioning.
- Extended access audit taxonomy with tenant lifecycle, tenant config, case member, and annotation operation families while preserving existing operation constants and delete event IDs.
- Wrapped tenant lifecycle/configuration, case-member, annotation, memory-unit delete, case delete, and tenant delete endpoints in `EndpointTelemetryScope` with principal-derived users and bounded query params.
- Deferred A41 access-telemetry retention/TTL as `20.5-A41-ACCESS-TELEMETRY-RETENTION` in `_bmad-output/implementation-artifacts/deferred-work.md`; A41 retention is not claimed closed by this story.
- Review fix: shared one `InboundRequestRateLimiter` partition store between ASP.NET Core global middleware and body-bound ingest endpoint filters so a tenant has one inbound quota across both surfaces.
- Review fix: preserved structured `ErrorResponse.Code` in display-name update audit events instead of generic status-derived codes.

### File List

- `_bmad-output/implementation-artifacts/20-5-inbound-rate-limiting-quotas-and-audit-completeness.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Memories.Server/Program.cs`
- `src/Hexalith.Memories.Server/RateLimiting/InboundRateLimitEndpointFilter.cs`
- `src/Hexalith.Memories.Server/RateLimiting/InboundRateLimitOptions.cs`
- `src/Hexalith.Memories.Server/RateLimiting/InboundRequestRateLimiter.cs`
- `src/Hexalith.Memories.Server/Telemetry/AccessTelemetryLog.cs`
- `src/Hexalith.Memories.Server/Telemetry/EndpointTelemetryScope.cs`
- `src/Hexalith.Memories.Server/Telemetry/TelemetryMetricsRecorder.cs`
- `src/Hexalith.Memories.Server/appsettings.Development.json`
- `src/Hexalith.Memories.Server/appsettings.Production.json`
- `src/Hexalith.Memories.Server/appsettings.json`
- `src/Hexalith.Memories.Telemetry/MemoriesMeter.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointRateLimitTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/AccessTelemetryLogTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/EndpointTelemetryScopeTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/MemoriesMetricsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/MutationAuditLogStreamTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Telemetry/TelemetryMetricsRecorderTests.cs`

### Senior Developer Review (AI)

Outcome: Approved after automatic fixes. No critical issues remain.

Issues found and fixed:

- HIGH: Body-bound ingest endpoints used an endpoint-filter limiter instance separate from the ASP.NET Core global limiter, allowing a tenant to consume one full quota on route/query API traffic plus another full quota on `/api/ingest*` body-bound traffic in the same window. Fixed by backing both global middleware partitions and the body-bound endpoint filter with the same singleton `InboundRequestRateLimiter`, and added `BodyBoundIngestEndpoint_SharesTenantQuotaWithRouteAndQueryApiTraffic`.
- MEDIUM: The display-name update audit wrapper converted structured handler failures to generic status-derived codes such as `HTTP_400`, losing the `ErrorResponse.Code` required for audit completeness. Fixed `MarkAuditFromHttpResult` to extract `ErrorResponse.Code` from typed results before falling back to status mapping, and asserted `INVALID_INPUT` in `MutationAuditLogStreamTests`.

Validation:

- `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --disable-build-servers -m:1 /nr:false` passed.
- Focused xUnit class run passed: 133 total, 0 failed.

### Change Log

- 2026-07-04: Story created via BMAD create-story workflow.
- 2026-07-04: Implemented inbound request rate limiting, low-cardinality rejection metrics, expanded mutation audit coverage, focused tests, and A41 retention deferred-work tracking.
- 2026-07-04: Story-automator review fixed shared quota enforcement across middleware/filter limiter paths and preserved structured audit error codes for display-name update failures; status set to done.
