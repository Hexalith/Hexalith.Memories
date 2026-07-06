---
title: '25.2 Error & Telemetry Centralization'
type: 'refactor'
created: '2026-07-06'
status: 'done'
baseline_revision: '3f906e2429877660165f03ca89e82c11e072f222'
final_revision: '709e2e383dcbb9a37cbb5bcc2224d3f47c13f5ec'
review_loop_iteration: 0
followup_review_recommended: true
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-25-context.md']
warnings: ['oversized']
---

<intent-contract>

## Intent

**Problem:** Endpoint error envelopes, tenant-id checks, tenant-active checks, activity scopes, audit scopes, and metric callbacks are repeated across the decomposed endpoint classes. This makes backend-unavailable, tenant-state, and telemetry behavior easy to drift, and the server still lacks a single `IExceptionHandler` that returns the public `ErrorResponse` envelope for unhandled endpoint failures.

**Approach:** Add reusable server endpoint infrastructure for structured results, tenant validation/status endpoint filters, endpoint telemetry descriptors/filters, and one sanitized exception handler, then migrate endpoint classes to those helpers without changing public routes, status codes, response bodies, headers, audit event IDs, metric names, or special degradation behavior.

## Boundaries & Constraints

**Always:** Preserve the `ErrorResponse` JSON shape; existing route templates and auth/rate-limit behavior; tenant status semantics (`TENANT_NOT_FOUND` -> 404, non-active tenant states -> 409); exists-only diagnostic routes; search degradation response codes, `Retry-After: 5`, and graph-vs-inner-search distinctions; graph traversal degraded `200 OK`; memory-unit lookup 503 on backend failure; export validation before `Response.StartAsync`; rate-limit `429` body/headers/metrics; `EndpointTelemetryScope` event IDs and outcome rules; low-cardinality metric tags; trace/audit correlation.

**Block If:** Implementation discovers a required public contract change, route rename, route versioning, changed auth policy, changed tenant-id syntax, changed search/graph/export degradation semantics, or a need to remove an existing endpoint behavior. Those decisions belong outside this centralization story.

**Never:** Do not flatten search, graph, export, or rate-limit special cases into a generic error mapper; do not expose stack traces, raw backend exception payloads, bearer tokens, cursor values, raw query text, or unbounded tag values; do not add controllers or new packages; do not touch submodules; do not change CLI or MCP behavior in this story.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Invalid tenant id | Any migrated endpoint receives a tenant id rejected by `TenantIdGuard` | Returns the same `400` `INVALID_TENANT_ID` envelope and rejected-tenant telemetry as before | Central tenant-id filter/factory owns the envelope; auth filter's broader tenant-claim syntax remains unchanged |
| Non-active tenant | Active-only endpoint receives a provisioning, deleting, failed, unavailable, or missing tenant | Missing tenant returns `404`; other non-active states return `409`; operation body is not executed | Central tenant-status filter uses `TenantStatusGuard` and `ErrorResults` |
| Exists-only diagnostic route | Consistency/status/verification route receives a non-active but registered tenant | Route still runs because only existence is required | Filter configuration must not force active status on diagnostic endpoints |
| Backend unavailable | DAPR, Redis, RediSearch, Redis Vector, or FalkorDB throws on a migrated endpoint | Existing status code, error code, message/suggestion, retry header, and degradation shape remain unchanged | Shared factories cover common envelopes; search/graph/export-specific mappers remain specific |
| Unhandled exception | Endpoint delegate throws after filters start telemetry and before response starts | Server returns sanitized `500` `UNHANDLED_EXCEPTION` `ErrorResponse` and emits one correlated audit event | Exception handler owns envelope; telemetry filter marks the scope and rethrows |
| Response already started | Export streaming fails after headers/body have started | Existing mid-stream behavior is not converted to JSON | Exception handler must not write a new envelope after `Response.HasStarted` |

</intent-contract>

## Code Map

- `src/Hexalith.Memories.Server/Endpoints/ErrorResults.cs` -- new central factory for common `ErrorResponse` values and `IResult` mappings used by endpoints, filters, rate limiting, and exception handling.
- `src/Hexalith.Memories.Server/Endpoints/EndpointValidationHelpers.cs` -- extend the existing helper to resolve tenant ids from route/query/body inputs and delegate envelope creation to `ErrorResults`.
- `src/Hexalith.Memories.Server/Endpoints/TenantIdValidationEndpointFilter.cs` -- new endpoint filter for `TenantIdGuard` validation before endpoint bodies run.
- `src/Hexalith.Memories.Server/Endpoints/TenantStatusEndpointFilter.cs` -- new endpoint filter for active-only and exists-only tenant registry checks through `TenantStatusGuard`.
- `src/Hexalith.Memories.Server/Telemetry/EndpointTelemetryDescriptor.cs` -- new descriptor for operation type, activity name, event IDs, case/source/axis metadata, query params, and optional metric callback.
- `src/Hexalith.Memories.Server/Telemetry/EndpointTelemetryFilter.cs` -- new endpoint filter that starts the activity, creates `EndpointTelemetryScope`, marks returned errors, marks exceptions, and guarantees one audit event.
- `src/Hexalith.Memories.Server/Endpoints/EndpointTelemetryHelpers.cs` -- keep small descriptor/query-param helpers; remove duplicate activity/scope setup from endpoint classes.
- `src/Hexalith.Memories.Server/Telemetry/EndpointTelemetryScope.cs` -- preserve audit event routing and outcome semantics; only add narrow support needed by the filter.
- `src/Hexalith.Memories.Telemetry/MemoriesActivitySource.cs` -- add constants for currently hard-coded tenant lifecycle/config, case-member, and annotation activity names.
- `src/Hexalith.Memories.Server/Diagnostics/MemoriesServerExceptionHandler.cs` -- new `IExceptionHandler` for sanitized `ErrorResponse` output when the response has not started.
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs` -- register the exception handler and reusable endpoint filters/descriptors.
- `src/Hexalith.Memories.Server/Program.cs` -- add exception-handler middleware without changing the relative order of authentication, authorization, tenant middleware, rate limiting, controller mapping, DAPR subscription mapping, or endpoint registration.
- `src/Hexalith.Memories.Server/Endpoints/{Ingestion,TenantLifecycle,Cases,Search,Graph,Consistency,Export}Endpoints.cs` -- migrate repeated tenant validation, tenant status, error result, activity, audit, and metric setup to shared infrastructure while preserving endpoint-local business logic.
- `src/Hexalith.Memories.Server/Endpoints/MemoryUnitLookupEndpoint.cs` -- migrate case-access telemetry and common result helpers while preserving `LOOKUP_BACKEND_UNAVAILABLE`.
- `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs` -- remove local duplicate tenant-id, tenant-not-found, and DAPR-unavailable factories.
- `src/Hexalith.Memories.Server/Search/SearchEndpointErrorResponseFactory.cs` and `src/Hexalith.Memories.Server/Search/SearchEndpointDegradationResponses.cs` -- either delegate common envelopes to `ErrorResults` or remain as search-specific adapters; preserve search-specific tests.
- `src/Hexalith.Memories.Server/RateLimiting/InboundRateLimitEndpointFilter.cs` and `src/Hexalith.Memories.Server/Hosting/InboundRateLimitPartitionFactory.cs` -- share the rate-limit result factory while preserving partition keys, retry headers, and metrics.
- `tests/Hexalith.Memories.Server.Tests/Endpoints/ErrorResultsTests.cs` -- new focused coverage for common factory codes, messages, suggestions, status codes, and retry headers.
- `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEndpointFilterTests.cs` -- new focused coverage for tenant-id, active-only, exists-only, and rejected-tenant telemetry behavior.
- `tests/Hexalith.Memories.Server.Tests/Telemetry/EndpointTelemetryFilterTests.cs` -- new focused coverage for success, returned `ErrorResponse`, exception path, exactly-one audit event, activity tags, and metric callback behavior.
- `tests/Hexalith.Memories.Server.Tests/Diagnostics/MemoriesServerExceptionHandlerTests.cs` -- new focused coverage for sanitized exception envelopes and response-start handling.
- `tests/Hexalith.Memories.Server.Tests/Telemetry/MemoriesActivitySourceTests.cs` -- update pinned activity-name constants.
- `tests/Hexalith.Memories.Server.Tests/Architecture/EndpointCentralizationGuardTests.cs` -- add guard coverage that endpoint classes no longer directly create telemetry scopes or start endpoint activities except explicit allowlisted infrastructure.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Memories.Server/Endpoints/ErrorResults.cs` -- add central factory methods for invalid tenant id, tenant forbidden, tenant not found/status, invalid input, not found, conflict, DAPR unavailable, backend unavailable, rate limit exceeded, lookup backend unavailable, export backend unavailable, and unhandled exception results -- removes repeated envelope construction without changing public shape.
- [x] `src/Hexalith.Memories.Server/Endpoints/EndpointValidationHelpers.cs` -- consolidate tenant-id resolution and `TenantIdGuard` validation for route, query, and body sources -- gives tenant filters one source for syntax validation.
- [x] `src/Hexalith.Memories.Server/Endpoints/TenantIdValidationEndpointFilter.cs` and `src/Hexalith.Memories.Server/Endpoints/TenantStatusEndpointFilter.cs` -- implement reusable endpoint filters and attach them to the existing route groups/endpoints with active-only or exists-only mode as appropriate -- moves tenant validation before endpoint bodies while preserving diagnostic exceptions.
- [x] `src/Hexalith.Memories.Server/Telemetry/EndpointTelemetryDescriptor.cs`, `src/Hexalith.Memories.Server/Telemetry/EndpointTelemetryFilter.cs`, and `src/Hexalith.Memories.Server/Endpoints/EndpointTelemetryHelpers.cs` -- centralize endpoint activity/scope setup, returned-result marking, exception marking, query-param population, and metric callbacks -- removes manual telemetry boilerplate from endpoint delegates.
- [x] `src/Hexalith.Memories.Telemetry/MemoriesActivitySource.cs` and `tests/Hexalith.Memories.Server.Tests/Telemetry/MemoriesActivitySourceTests.cs` -- add and pin constants for tenant lifecycle, tenant config, case-member, and annotation activities -- prevents hard-coded activity-name drift.
- [x] `src/Hexalith.Memories.Server/Diagnostics/MemoriesServerExceptionHandler.cs`, `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs`, and `src/Hexalith.Memories.Server/Program.cs` -- register and enable one exception handler that returns sanitized `ErrorResponse` envelopes before responses start -- closes the unhandled-exception gap.
- [x] `src/Hexalith.Memories.Server/Endpoints/{Ingestion,TenantLifecycle,Cases,Search,Graph,Consistency,Export}Endpoints.cs`, `src/Hexalith.Memories.Server/Endpoints/MemoryUnitLookupEndpoint.cs`, and `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs` -- migrate endpoint-local duplicates to `ErrorResults`, tenant filters, and telemetry descriptors -- preserves behavior while reducing repeated code.
- [x] `src/Hexalith.Memories.Server/Search/SearchEndpointErrorResponseFactory.cs`, `src/Hexalith.Memories.Server/Search/SearchEndpointDegradationResponses.cs`, `src/Hexalith.Memories.Server/RateLimiting/InboundRateLimitEndpointFilter.cs`, and `src/Hexalith.Memories.Server/Hosting/InboundRateLimitPartitionFactory.cs` -- reuse common factories only where semantics are identical -- keeps search/degradation/rate-limit special cases explicit.
- [x] `tests/Hexalith.Memories.Server.Tests/Endpoints/ErrorResultsTests.cs`, `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEndpointFilterTests.cs`, `tests/Hexalith.Memories.Server.Tests/Telemetry/EndpointTelemetryFilterTests.cs`, `tests/Hexalith.Memories.Server.Tests/Diagnostics/MemoriesServerExceptionHandlerTests.cs`, and `tests/Hexalith.Memories.Server.Tests/Architecture/EndpointCentralizationGuardTests.cs` -- add focused tests for the new infrastructure and drift guards -- proves centralization without relying only on E2E coverage.
- [x] `tests/Hexalith.Memories.Server.Tests/{Endpoints,Telemetry,Authentication,Tenants,Search,Graph}/**/*.cs` -- update existing focused tests only for implementation details, not expected behavior -- ensures the refactor is behavior-preserving.

**Acceptance Criteria:**
- Given any migrated endpoint rejects a malformed tenant id, when the request executes, then the response remains `400` with `INVALID_TENANT_ID` and telemetry records the synthetic rejected tenant tag.
- Given an active-only endpoint receives a missing or non-active tenant, when the tenant-status filter runs, then missing returns `404` and non-active statuses return `409` with the existing `TenantStatusGuard` codes.
- Given an exists-only diagnostic endpoint receives a registered non-active tenant, when the request executes, then the endpoint body still runs and does not fail solely because the tenant is not active.
- Given search, graph, memory-unit lookup, export, and rate-limit backend failures, when the existing focused tests run, then status codes, error codes, retry headers, degraded payloads, and metric/audit behavior match the pre-refactor expectations.
- Given an endpoint throws an unhandled exception before the response starts, when the exception handler runs, then the client receives a sanitized `500` `UNHANDLED_EXCEPTION` `ErrorResponse` and one correlated audit event is emitted.
- Given the endpoint classes are scanned, when the centralization guard runs, then endpoint-local `new EndpointTelemetryScope`, endpoint-local endpoint activity `StartActivity`, and duplicate common `new ErrorResponse` patterns are absent except explicitly allowlisted special-case adapters.

## Spec Change Log

## Review Triage Log

### 2026-07-06 -- Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 10: (high 0, medium 5, low 5)
- defer: 0
- reject: 1: (high 0, medium 0, low 1)
- addressed_findings:
  - `[medium]` `[patch]` Tenant-id and tenant-status filters skipped blank or whitespace tenant ids; validation now rejects any resolved blank tenant id before endpoint bodies run, with focused tests for both filters.
  - `[medium]` `[patch]` Tenant resolution preferred query values before body-bound endpoint contracts; body-bound values now take precedence over query values, with coverage for mixed query/body inputs.
  - `[medium]` `[patch]` `EndpointTelemetryFilter` configured activities outside its guarded execution path; descriptor activity setup now runs inside the telemetry try/catch and records an error audit when setup fails.
  - `[medium]` `[patch]` Exception-handler fallback audit used the search audit channel for every operation; fallback audit now resolves operation-specific event ids and logger methods for ingest, traverse, tenant config, case access, deletion, annotations, and related operations.
  - `[medium]` `[patch]` Exception-handler fallback audit could tag raw invalid route/query tenant values; tenant tags are now normalized through `TenantIdGuard` or replaced with the rejected-tenant tag.
  - `[low]` `[patch]` Exception-handler fallback audit always used zero duration and tests did not assert fallback/skip behavior; duration is now derived from the current activity when available, and tests assert fallback audit, operation event ids, invalid-tenant normalization, and skip-after-endpoint-audit behavior.
  - `[low]` `[patch]` The endpoint telemetry guard only proved one filter usage; the guard now asserts production endpoint mappings use `EndpointTelemetryFilter.For(...)`.
  - `[low]` `[patch]` The common-error centralization guard did not catch repeated common `new ErrorResponse(...)` patterns; the guard now scans endpoint, hosting, diagnostics, and rate-limit code for duplicated shared error codes.
  - `[low]` `[patch]` Tenant filter tests lacked blank/whitespace tenant-id cases; focused tests now pin blank route tenant rejection for validation and status filters.
  - `[low]` `[patch]` `ErrorResults.SetRetryAfter` overwrote existing `Retry-After` headers; it now preserves existing values and has a regression test.

## Design Notes

Keep centralization layered rather than monolithic: `ErrorResults` should own common envelope/status construction; search and graph degradation adapters should keep their axis-specific policy and call `ErrorResults` only for identical envelopes; `EndpointTelemetryFilter` should wrap endpoint execution but leave business-specific result shaping inside the endpoint.

Endpoint telemetry descriptors should make endpoint registration readable:

```csharp
group.MapPost("/api/search", SearchAsync)
    .AddEndpointFilter(EndpointTelemetryFilter.For(SearchTelemetryDescriptor));
```

Do not make telemetry depend on result body string parsing. Returned `IValueHttpResult` values containing `ErrorResponse` and status-code interfaces are sufficient for common error marking; special cases can explicitly mark partial outcomes before returning.

## Implementation Notes

- Added central `ErrorResults`, tenant validation/status endpoint filters, telemetry descriptors/filter, and a sanitized `MemoriesServerExceptionHandler`.
- Migrated common endpoint error envelopes, tenant-status checks, rate-limit responses, lookup/backend-unavailable responses, and tenant lifecycle/config/case-member/annotation activity names to shared infrastructure while keeping search, graph, export, and degradation-specific behavior explicit.
- Registered the exception handler and the embedding-config update endpoint's common filters without changing route templates, auth policies, middleware order, or existing special-case response shapes.
- Added architecture guard tests for telemetry centralization and common error-envelope drift.
- Review patches tightened tenant-id resolution precedence, blank tenant handling, telemetry filter exception coverage, fallback audit event routing, tenant tag normalization, activity duration capture, and `Retry-After` preservation.

## Verification

**Commands:**
- `dotnet build Hexalith.Memories.slnx` -- expected: succeeds with zero warnings.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~ErrorResultsTests|FullyQualifiedName~TenantEndpointFilterTests|FullyQualifiedName~EndpointTelemetryFilterTests|FullyQualifiedName~MemoriesServerExceptionHandlerTests|FullyQualifiedName~EndpointCentralizationGuardTests"` -- expected: new centralization tests pass.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~EndpointTelemetryScopeTests|FullyQualifiedName~AccessTelemetryLogTests|FullyQualifiedName~MemoriesActivitySourceTests|FullyQualifiedName~TelemetryMetricsRecorderTests|FullyQualifiedName~TracePropagationNoDockerTests|FullyQualifiedName~AuditLogStreamTests|FullyQualifiedName~MutationAuditLogStreamTests|FullyQualifiedName~ServerEndpointRateLimitTests"` -- expected: telemetry, audit, and rate-limit behavior remains pinned.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~TenantStatusGuardTests|FullyQualifiedName~TenantContextEnforcementTests|FullyQualifiedName~TenantConfigurationEndpointTests|FullyQualifiedName~TenantAuthorizationEndpointFilterTests|FullyQualifiedName~ServerEndpointAuthorizationTests"` -- expected: tenant status, authorization, and validation behavior remains unchanged.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~SearchEndpointErrorResponseFactoryTests|FullyQualifiedName~SearchEndpointDegradationTests|FullyQualifiedName~SearchEndpointContractTests|FullyQualifiedName~TraverseEndpointTokenBudgetTests|FullyQualifiedName~MemoryUnitLookupEndpointTests|FullyQualifiedName~ConsistencyEndpointTests|FullyQualifiedName~DirectoryIngestionEndpointE2ETests|FullyQualifiedName~CaseMutationEndpointE2ETests"` -- expected: endpoint-specific error and degradation behavior remains unchanged.
- `git diff --check -- src/Hexalith.Memories.Server src/Hexalith.Memories.Telemetry tests/Hexalith.Memories.Server.Tests _bmad-output/implementation-artifacts/spec-25-2-error-and-telemetry-centralization.md` -- expected: no whitespace errors.

**Results:**
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~ErrorResultsTests|FullyQualifiedName~TenantEndpointFilterTests|FullyQualifiedName~EndpointTelemetryFilterTests|FullyQualifiedName~MemoriesServerExceptionHandlerTests|FullyQualifiedName~EndpointCentralizationGuardTests"` -- passed, 29 tests.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~EndpointTelemetryScopeTests|FullyQualifiedName~AccessTelemetryLogTests|FullyQualifiedName~MemoriesActivitySourceTests|FullyQualifiedName~TelemetryMetricsRecorderTests|FullyQualifiedName~TracePropagationNoDockerTests|FullyQualifiedName~AuditLogStreamTests|FullyQualifiedName~MutationAuditLogStreamTests|FullyQualifiedName~ServerEndpointRateLimitTests"` -- passed, 105 tests.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~TenantStatusGuardTests|FullyQualifiedName~TenantContextEnforcementTests|FullyQualifiedName~TenantConfigurationEndpointTests|FullyQualifiedName~TenantAuthorizationEndpointFilterTests|FullyQualifiedName~ServerEndpointAuthorizationTests"` -- passed, 70 tests.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~SearchEndpointErrorResponseFactoryTests|FullyQualifiedName~SearchEndpointDegradationTests|FullyQualifiedName~SearchEndpointContractTests|FullyQualifiedName~TraverseEndpointTokenBudgetTests|FullyQualifiedName~MemoryUnitLookupEndpointTests|FullyQualifiedName~ConsistencyEndpointTests|FullyQualifiedName~DirectoryIngestionEndpointE2ETests|FullyQualifiedName~CaseMutationEndpointE2ETests"` -- passed, 79 tests.
- `dotnet build Hexalith.Memories.slnx` -- passed, 0 warnings, 0 errors.
- `git diff --check -- src/Hexalith.Memories.Server src/Hexalith.Memories.Telemetry tests/Hexalith.Memories.Server.Tests _bmad-output/implementation-artifacts/spec-25-2-error-and-telemetry-centralization.md` -- passed.

## Auto Run Result

Status: done

Summary: Centralized common endpoint error envelopes, tenant validation/status checks, endpoint telemetry setup, rate-limit/backend-unavailable result construction, and sanitized unhandled-exception responses while preserving existing route contracts and endpoint-specific degradation behavior.

Files changed:
- `src/Hexalith.Memories.Server/Endpoints/ErrorResults.cs` -- added shared factories for common `ErrorResponse` results and retry-header behavior.
- `src/Hexalith.Memories.Server/Endpoints/TenantIdValidationEndpointFilter.cs`, `src/Hexalith.Memories.Server/Endpoints/TenantStatusEndpointFilter.cs`, and `src/Hexalith.Memories.Server/Endpoints/EndpointValidationHelpers.cs` -- added reusable tenant-id and tenant-status validation with route/body/query resolution.
- `src/Hexalith.Memories.Server/Telemetry/EndpointTelemetryDescriptor.cs`, `src/Hexalith.Memories.Server/Telemetry/EndpointTelemetryFilter.cs`, and `src/Hexalith.Memories.Server/Endpoints/EndpointTelemetryHelpers.cs` -- centralized reusable endpoint activity, audit, metric, and error-marking behavior.
- `src/Hexalith.Memories.Server/Diagnostics/MemoriesServerExceptionHandler.cs`, `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs`, and `src/Hexalith.Memories.Server/Program.cs` -- registered sanitized exception handling without changing existing middleware order.
- `src/Hexalith.Memories.Server/Endpoints/*Endpoints.cs`, `src/Hexalith.Memories.Server/Search/*`, `src/Hexalith.Memories.Server/RateLimiting/*`, `src/Hexalith.Memories.Server/Hosting/InboundRateLimitPartitionFactory.cs`, and `src/Hexalith.Memories.Server/Tenants/*` -- migrated duplicated common envelopes and telemetry setup while keeping special-case endpoint behavior explicit.
- `src/Hexalith.Memories.Telemetry/MemoriesActivitySource.cs` -- pinned new endpoint activity-name constants.
- `tests/Hexalith.Memories.Server.Tests/**` -- added focused infrastructure tests, exception-handler tests, centralization guards, and regression coverage for behavior-preserving migration.

Review findings breakdown: patched 10 findings (5 medium, 5 low), deferred 0 findings, rejected 1 low finding. Follow-up review recommended: true.

Verification performed: centralization tests passed 29/29; telemetry/audit/rate-limit tests passed 105/105; tenant/auth tests passed 70/70; endpoint-specific degradation/error tests passed 79/79; `dotnet build Hexalith.Memories.slnx` passed with 0 warnings and 0 errors; scoped `git diff --check` passed.

Residual risks: Full Docker/Aspire integration lanes were not run. A follow-up review is recommended because the review pass changed behavior-affecting tenant validation and telemetry/audit fallback paths after the implementation pass.
