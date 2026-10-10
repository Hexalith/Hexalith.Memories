---
title: 'Requalify Story 5.4 tenant context enforcement'
type: 'chore'
created: '2026-10-10'
status: 'done'
baseline_commit: '26d69d4f111d341e02c24d5c2c597cf128e40bf9'
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
- [x] `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantContextEnforcementIntegrationTests.cs` -- extend the existing sidecar test to assert missing and wrong tokens fail and a valid token succeeds in enabled mode; retain default-mode behavior.
- [x] `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantContextEnforcementIntegrationTests.cs` -- build its project and run only the edited method with test-only tokens and Aspire; inspect the real sidecar status. If transport fails, correct `Fixtures/AspireIngestionPipelineFixture.cs` and rerun.
- [x] `_bmad-output/implementation-artifacts/spec-5-4-tenant-context-requalification.md` -- record exact results, tenant-graph call-site audit, and any blocker or fix.

**Acceptance Criteria:**
- Given enabled token mode, when the direct sidecar test runs, then missing/wrong tokens are denied and the valid token reaches metadata.
- Given the focused tenant and graph tests, when run, then caller denials and parameterized tenant-scoped graph behavior pass without cross-tenant disclosure.

## Implementation Notes

- The direct metadata test now sends three requests to the fixture-resolved daprd HTTP endpoint when token mode is enabled: no `dapr-api-token`, a deliberately wrong token, and the fixture's configured valid token. It asserts 401/403, 401/403, and 200 respectively. The token-free branch retains its 200 assertion.
- The fixture's direct Memories and MCP application clients now attach `APP_API_TOKEN` when configured. In token mode those clients otherwise bypass daprd and receive 401 from `DaprApplicationTokenMiddleware` during fixture startup. No production authentication or token defaults changed.
- The existing planted-corruption integration test now asserts the 404 error code, absence of tenant B and stored payload in the response, and a captured Critical `TENANT_MISMATCH` event naming the requested tenant A, stored tenant B, and memory-unit ID. This is one request against the corrupted Redis hash, not separate unit-level inferences.
- A real-FalkorDB test now seeds an injection-shaped value in A and a private value in B through parameterized builder calls, counts both graphs, and verifies that A's graph contains no B node.
- Current tenant-graph audit: `rg -n 'SelectGraph\(|GRAPH\.QUERY|GRAPH\.RO_QUERY|GRAPH\.DELETE' src --glob '*.cs'` found 51 `SelectGraph` calls across 26 Server files. Each selects `tenantId`, `input.TenantId`, or `graphId` assigned from one of those tenant values. This includes indexing, search, traversal, case operations, consistency/repair, restore/import/export, derived-store correction, tenant lifecycle and metrics, and the search annotation-count path. The sole direct `GRAPH.DELETE` passes `input.TenantId`; `GRAPH.LIST` callers are infrastructure probes. Builder-generated data queries pass values through parameter dictionaries; the migration gate/backfill and lifecycle/metrics probes use constant Cypher (with parameters where values exist). No raw user value concatenation or tenant graph selection defect was found, so no graph runtime change was needed.
- The standalone `aspire run` baseline did not reach a ready Memories resource: `aspire describe` showed Redis, FalkorDB, and OpenBao healthy, `security` unhealthy, and Memories waiting. The test fixture disables Keycloak and the token-enabled focused test did reach its real sidecar.
- A token-free repeat was attempted with `env -u DAPR_API_TOKEN_MODE -u DAPR_API_TOKEN -u APP_API_TOKEN ... -method ...`; the fixture's EventStore gateway exited with `Authentication:JwtBearer requires either 'Authority' (production OIDC) or 'SigningKey' (development symmetric key) to be configured.` A second token-free attempt supplied the test JWT key, issuer, and audience; it still did not finish fixture startup within 65 seconds (`timeout` exit 124, xUnit `Total: 0`). Default-mode real-sidecar behavior remains unverified in this run. Fixture/AppHost integration ownership should reopen this check when startup is stable.
- For the additional matrix runs, supplying test-only `Authentication__JwtBearer__SigningKey`, `Issuer`, and `Audience` to the process removed the EventStore JWT startup failure. The corrupted-record test completed. The existing two-tenant HTTP graph test still produced no test result within a bounded 120-second run after Aspire started its application resources; it was stopped (`Total: 0`). Its endpoint traversal assertions remain unexecuted in this pass. The real-FalkorDB test proves graph separation and parameter binding below the HTTP caller, while the source audit above covers tenant graph selection at call sites.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 token-free branch unexecuted | medium | defer | The branch at `TenantContextEnforcementIntegrationTests.cs:225` is unchanged, but local fixture startup prevented execution; a second run with test JWT settings timed out at 65 seconds with `Total: 0`. The default-mode lane predates this change. |
| B2 HTTP graph path unexecuted | medium | defer | `TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes` did not reach its body in the bounded Aspire runs. The direct FalkorDB test covers graph separation, but cannot prove authenticated endpoint graph choice. This HTTP fixture gap predates the requalification. |
| B3 graph check only A excludes B | low | patch | `GraphQueryBuilderIntegrationTests.cs:170` checks B's node ID in A, while B could still contain A's ID. Add the symmetric count assertion. |
| B4 hostile content not read back | medium | patch | `GraphQueryBuilderIntegrationTests.cs:151` verifies the bound value before execution and node counts after, but could miss changed stored content. Read the exact A and B content back. |
| B5 missing live app-token denial check | medium | defer | The fixture now supplies the valid app token for direct clients; middleware unit tests cover missing and wrong tokens, but no live non-health-route denial ran. That live negative proof was absent before this test change. |
| B6 response code matched as text | low | patch | `TenantContextEnforcementIntegrationTests.cs:155` could find `MEMORY_UNIT_NOT_FOUND` outside the error `Code` field. Deserialize `ErrorResponse` and assert `Code`. |
| B7 mismatch log match too broad | low | patch | `TenantContextEnforcementIntegrationTests.cs:162` accepts any captured category and a non-Critical entry containing a Critical JSON fragment. Bind the match to the Memories resource and actual Critical level or exact serialized event 5400. |
| V1 enabled token branch absent from routine CI | medium | defer | CI and nightly run the integration filter without `DAPR_API_TOKEN_MODE=enabled`; the prior version already had an enabled-only sidecar assertion. The new assertions were run manually, but an enabled routine lane remains a separate pre-existing gate gap. |
| V2 live graph content not read back | medium | patch | The executed graph test checks query text, parameters, and counts, so altered stored hostile content would pass. Same root cause as B4; assert exact persisted content for both graphs. |
| V3 MCP direct transports omit app token | medium | defer | `McpServerIntegrationTests` and `McpAuthenticationIntegrationTests` construct their own transports without `dapr-api-token`; they predate this fixture change and were not part of the focused sidecar run. Token-mode MCP transport coverage needs separate ownership. |

## Verification

**Commands:**
- `aspire run` then `aspire describe` -- Redis, FalkorDB, OpenBao healthy; security unhealthy; Memories waiting. Stopped the standalone AppHost before the integration test.
- `dotnet restore tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1` -- succeeded after the first test attempt exposed stale 3.117.1 integration assets against AppHost's EventStore.Aspire 3.119.0.
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --configuration Debug --no-restore -m:1` -- succeeded, 0 warnings, 0 errors.
- `env DAPR_API_TOKEN_MODE=enabled DAPR_API_TOKEN=story54-test-sidecar-token APP_API_TOKEN=story54-test-app-token dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantContextEnforcementIntegrationTests.DaprSidecar_RequestWithoutApiToken_IsRejected` -- passed 1/1, 0 skipped, in the final 9.357s run. The test asserted missing token 401/403, wrong token 401/403, and valid token 200 against the real sidecar. Diagnostic direct HTTP probes observed 401, 401, and 200 respectively.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug -m:1` -- succeeded, 0 warnings, 0 errors. Built test assembly runs with `-class Hexalith.Memories.Server.Tests.<class>` passed: `Tenants.TenantContextEnforcementTests` 17/17; `Authentication.ServerEndpointAuthorizationTests` 31/31; `Authentication.DaprApplicationTokenMiddlewareTests` 6/6; `Authentication.DaprTokenStartupValidatorTests` 3/3; `Graph.GraphQueryBuilderTests` 138/138. Total 195/195. Negative evidence includes `TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState` and the graph builder `InjectionPrevention_*` tests.
- `env -u DAPR_API_TOKEN_MODE -u DAPR_API_TOKEN -u APP_API_TOKEN dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantContextEnforcementIntegrationTests.DaprSidecar_RequestWithoutApiToken_IsRejected` -- infrastructure-blocked before execution, then stopped (`Total: 0`): EventStore gateway missing JWT Authority/SigningKey as described above.
- `timeout -s INT 65s env -u DAPR_API_TOKEN_MODE -u DAPR_API_TOKEN -u APP_API_TOKEN Authentication__JwtBearer__SigningKey=<test key> Authentication__JwtBearer__Issuer=<test issuer> Authentication__JwtBearer__Audience=<test audience> dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantContextEnforcementIntegrationTests.DaprSidecar_RequestWithoutApiToken_IsRejected` -- exit 124, xUnit `Total: 0`; fixture startup did not finish, without the prior JWT configuration error.
- `env DAPR_API_TOKEN_MODE=enabled DAPR_API_TOKEN=story54-test-sidecar-token APP_API_TOKEN=story54-test-app-token Authentication__JwtBearer__SigningKey=<test key> Authentication__JwtBearer__Issuer=<test issuer> Authentication__JwtBearer__Audience=<test audience> timeout -s INT 55s dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantContextEnforcementIntegrationTests.MemoryUnit_CorruptedTenantId_Returns404WithoutLeakingData` -- passed 1/1, 0 skipped, 9.511s. The same request returned `404 MEMORY_UNIT_NOT_FOUND` without the foreign tenant or payload, and AppHost captured event 5400, `LogLevel: Critical`, `TENANT_MISMATCH` with requested tenant A, actual tenant B, and `mu-xyz`.
- `timeout 40s dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Graph.GraphQueryBuilderIntegrationTests.TenantIsolation_SeparateGraphs_ShouldNotLeakData` -- passed 1/1, 0 skipped, 3.312s against real FalkorDB: hostile A content absent from query text and present in `content` parameter; A and B each had one node in separate graphs; A graph returned count 0 for B's node ID.
- The token-enabled Aspire `TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes` was attempted with the same test-only JWT settings and `timeout -s INT 120s`; Aspire started Memories, MCP, and EventStore, but the fixture did not finish initialization before the bound. Exit 124, xUnit `Total: 0`; no HTTP graph assertion executed. Earlier bounded 55s and 90s attempts also stopped before the test body (the first two runs logged the EventStore JWT failure). This is a remaining endpoint-level evidence gap; the real-FalkorDB test above covers the graph matrix row.
- `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --no-restore -v:q` -- succeeded after the matrix test edits, 0 warnings, 0 errors.
- After review fixes, the integration project built with 0 warnings/errors and `GraphQueryBuilderIntegrationTests.TenantIsolation_SeparateGraphs_ShouldNotLeakData` passed 1/1 with bidirectional node exclusion and exact A/B content readback.
- `timeout -s INT 90s env DAPR_API_TOKEN_MODE=enabled DAPR_API_TOKEN=story54-test-sidecar-token APP_API_TOKEN=story54-test-app-token Authentication__JwtBearer__SigningKey=<test key> Authentication__JwtBearer__Issuer=<test issuer> Authentication__JwtBearer__Audience=<test audience> dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantContextEnforcementIntegrationTests.MemoryUnit_CorruptedTenantId_Returns404WithoutLeakingData` -- passed 1/1, 0 skipped, 11.078s after review fixes; the captured log showed event 5400 at Critical.
- `timeout -s INT 75s env DAPR_API_TOKEN_MODE=enabled DAPR_API_TOKEN=story54-test-sidecar-token APP_API_TOKEN=story54-test-app-token Authentication__JwtBearer__SigningKey=<test key> Authentication__JwtBearer__Issuer=<test issuer> Authentication__JwtBearer__Audience=<test audience> dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Tenants.TenantContextEnforcementIntegrationTests.DaprSidecar_RequestWithoutApiToken_IsRejected` -- passed 1/1, 0 skipped, 11.958s after review fixes.
- `git diff --check` -- clean.
