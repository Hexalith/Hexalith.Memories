---
title: 'Requalify Story 5.4 tenant context enforcement'
type: 'chore'
created: '2026-10-10'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-5-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/5-4-tenant-context-enforcement.md'
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.4 is done, but the default integration run does not exercise Dapr's token-enabled denial path. The old epic's caller-facing `TENANT_MISMATCH` wording differs from today's secure response contract.

**Approach:** The user chose requalification against the current architecture. Strengthen one sidecar integration test, run focused tenant/graph tests and the token-enabled Aspire case, then record evidence. Fix a runtime defect only if these checks demonstrate one.

## Boundaries & Constraints

**Always:** Bearer claims authorize tenant scope. Keep caller-facing 403 `TENANT_FORBIDDEN`, authorized unknown-tenant 404 `TENANT_NOT_FOUND`, and internal Critical `TENANT_MISMATCH` for corrupt stored records. Production requires Dapr tokens; local/test default remains token-free. Graph calls select the tenant database and parameterize user data. Attach negative evidence for any changed isolation surface.

**Never:** Reveal foreign tenant data, change API error codes or production secrets, force tokens into all local tests, mutate production tenant data, or reset the completed story. Constant tenant-scoped infrastructure Cypher needs an audit, not a new abstraction.

## I/O & Edge-Case Matrix

| State | Action | Expected result |
|-------|--------|-----------------|
| Bearer tenant A requests B | Call tenant route | 403 `TENANT_FORBIDDEN`; no B evidence |
| A key contains B tenant field | Read record | 404; Critical mismatch signal |
| Token mode enabled | Call real sidecar metadata without, with wrong, then with valid token | 401/403, 401/403, then 200 |
| Tenant A graph and hostile value | Build/query graph | A graph; value bound; no B result |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Memories.Server/Tenants/TenantStatusGuard.cs:21` and `Endpoints/TenantLifecycleEndpoints.cs:53` -- registry guards; reuse.
- `src/Hexalith.Memories.Server/Authentication/TenantAuthorizationEndpointFilter.cs:43`, `TenantAuthorizationMiddleware.cs:24`, and `Endpoints/ErrorResults.cs:33` -- bearer tenant enforcement and 403 response; reuse.
- `src/Hexalith.Memories.Server/Cases/CaseService.cs:234` and `Tenants/TenantMismatchMonitor.cs:40` -- corrupt-record denial and Critical signal; reuse.
- `src/Hexalith.Memories.AppHost/Program.cs:1132` and `src/Hexalith.Memories.ServiceDefaults/Security/DaprTokenStartupValidator.cs:16` -- token-mode switch and production gate; preserve.
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:1170` -- valid-token sidecar client factory; reuse in the test.
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantContextEnforcementIntegrationTests.cs:175` -- extend the existing direct-sidecar test with wrong/valid token assertions.
- `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs` and `tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs` -- parameterized graph behavior; audit tenant selection at callers.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.Memories.Server.Tests/Tenants/TenantContextEnforcementTests.cs` and `Authentication/ServerEndpointAuthorizationTests.cs`, `Authentication/DaprApplicationTokenMiddlewareTests.cs`, `Authentication/DaprTokenStartupValidatorTests.cs`, `Graph/GraphQueryBuilderTests.cs` -- run the five focused classes; 195 passed before approval.
- [ ] `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantContextEnforcementIntegrationTests.cs` -- extend the existing sidecar test to assert missing and wrong tokens fail and a valid token succeeds in enabled mode; retain default-mode behavior.
- [ ] `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantContextEnforcementIntegrationTests.cs` -- build its project and run only the edited method with test-only tokens and Aspire; inspect the real sidecar status. If transport fails, correct `Fixtures/AspireIngestionPipelineFixture.cs` and rerun.
- [ ] `_bmad-output/implementation-artifacts/spec-5-4-tenant-context-requalification.md` -- record exact results, tenant-graph call-site audit, and any blocker or fix.

**Acceptance Criteria:**
- Given enabled token mode, when the direct sidecar test runs, then missing/wrong tokens are denied and the valid token reaches metadata.
- Given the focused tenant and graph tests, when run, then caller denials and parameterized tenant-scoped graph behavior pass without cross-tenant disclosure.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `aspire run` then `aspire describe` -- expected: AppHost resources reach their documented baseline before editing the test.
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Debug --no-restore -m:1` -- expected: clean build; already passed.
- `env DAPR_API_TOKEN_MODE=enabled DAPR_API_TOKEN=story54-test-sidecar-token APP_API_TOKEN=story54-test-app-token dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantContextEnforcementIntegrationTests.DaprSidecar_RequestWithoutApiToken_IsRejected` -- expected: three exact sidecar statuses above.
