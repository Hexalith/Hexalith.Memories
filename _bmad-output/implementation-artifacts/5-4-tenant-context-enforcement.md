# Story 5.4: Tenant Context Enforcement

Status: ready-for-dev

## Story

As an operator,
I want tenant context enforced at all access layers,
so that cross-tenant requests are structurally impossible, not just policy-prohibited.

## Acceptance Criteria

1. **Given** a request with a tenant ID in the payload, **when** the Memories Server processes it, **then** the tenant ID is validated against the tenant registry before any operation, **and** unknown tenant IDs are rejected with error code `TENANT_NOT_FOUND` and suggestion to list tenants (FR44).

2. **Given** a request authenticated as tenant A attempting to access tenant B, **when** the server processes it, **then** it is rejected with error code `TENANT_MISMATCH` and clear error message (FR44).

3. **Given** inter-service communication between Memories Server components, **when** any call is made, **then** DAPR API token authentication is required (NFR10), **and** unauthenticated requests are rejected.

4. **Given** all FalkorDB graph queries, **when** executed, **then** they are scoped to the tenant's dedicated database, **and** parameterized Cypher via `IGraphQueryBuilder` prevents query injection that could access other databases.

## Tasks / Subtasks

- [ ] Task 1: Systematic tenant registry validation on all endpoints (AC: #1)
  - [ ] 1.1 Audit `Program.cs` and identify all tenant-scoped endpoints that call `ValidateTenantId()` but NOT `TenantStatusGuard.ValidateTenantActiveAsync()`. Currently these endpoints only check format (regex + reserved names) but not registry existence:
    - `GET /api/tenants/{tenantId}/embedding-config` (line ~197)
    - `PUT /api/tenants/{tenantId}/embedding-config` (line ~212)
    - `POST /api/tenants/{tenantId}/provision-status/{instanceId}` (line ~332)
    - `GET /api/tenants/{tenantId}/deletion-status/{instanceId}` (line ~505)
    - `POST /api/tenants/{tenantId}/verify` (line ~529)
  - [ ] 1.2 Add `TenantStatusGuard.ValidateTenantActiveAsync()` calls to all identified endpoints. Use the same pattern as `/api/ingest` and `/api/search`:
    ```csharp
    ErrorResponse? tenantStatusError = await tenantGuard.ValidateTenantActiveAsync(tenantId, cancellationToken);
    if (tenantStatusError is not null)
    {
        return Results.Conflict(tenantStatusError);
    }
    ```
  - [ ] 1.3 Exception: The `POST /api/tenants/{tenantId}/verify` endpoint should allow non-Active tenants (verification should work on Provisioning, Failed, etc.) -- use the new `TenantStatusGuard.ValidateTenantExistsAsync()` method (see Task 1.4) for existence-only check without status enforcement.
  - [ ] 1.4 Exception: The `GET /api/tenants/{tenantId}/deletion-status/{instanceId}` endpoint must work for Deleting tenants. Add a new method `ValidateTenantExistsAsync(string tenantId, CancellationToken ct)` on `TenantStatusGuard` that checks existence only (not active status), returning `TENANT_NOT_FOUND` for missing tenants but allowing any status. This keeps validation responsibility centralized in `TenantStatusGuard` rather than scattering `TenantRegistryService.GetTenantAsync()` calls across endpoints. Use this same method for the verify endpoint (Task 1.3).
  - [ ] 1.5 Inject `TenantStatusGuard` into endpoint delegates where not already present. Some endpoints already have it (`/api/ingest`, `/api/search`, case endpoints). Only add to the 5 identified endpoints above.

- [ ] Task 2: Cross-tenant access mismatch detection (AC: #2)
  - [ ] 2.1 Identify cross-tenant mismatch vectors in the current API surface:
    - **Ingestion:** `POST /api/ingest` receives `IngestionInput.TenantId` in the body. No path-level tenantId to mismatch against (body is the sole source). No mismatch vector here.
    - **Case operations:** `POST /api/tenants/{tenantId}/cases` -- path tenantId vs `CreateCaseInput.TenantId` in body (if present). Currently `CreateCaseInput` does NOT include a tenantId field (the path tenantId is used), so no mismatch vector.
    - **Memory unit access:** `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` -- the memory unit's `TenantId` field could differ from the path tenantId if data corruption exists. The current code already validates via `caseService.GetMemoryUnitAsync(tenantId, ...)` which scopes by tenant.
    - **Search:** `GET /api/search?tenantId=...` -- single source, no mismatch.
    - **Graph traversal:** Search with `startNodeId` -- graph queries already scoped to tenant-specific FalkorDB database (AC4). No mismatch vector.
  - [ ] 2.2 Add `TENANT_MISMATCH` detection for the one remaining vector: **memory unit tenant ownership validation**. When a memory unit is retrieved and its `TenantId` field does not match the path/request tenantId, return:
    ```json
    {"code": "TENANT_MISMATCH", "message": "Memory unit '{id}' belongs to tenant '{actualTenantId}', not '{requestTenantId}'.", "suggestion": "Verify the tenant ID in your request."}
    ```
    This is a defense-in-depth check -- if the physical isolation is working, this should never trigger. But if it does, it prevents data leakage.
  - [ ] 2.3 Add `TENANT_MISMATCH` validation in `CaseService` methods that retrieve data: after fetching a `Case` or `MemoryUnit` by ID, verify its `TenantId` matches the requested tenant. Return null (treated as not-found) or throw if mismatch is detected. Log a `Critical` level message since this indicates an isolation breach. **Pre-check:** Verify that `MemoryUnit` record includes a `TenantId` field -- if it doesn't, the mismatch check has nothing to compare against and the field must be added to the contract first.
  - [ ] 2.4 Register error code `TENANT_MISMATCH` in the error response conventions documentation (inline comment in `Program.cs` alongside existing error codes).

- [ ] Task 3: DAPR API token authentication (AC: #3)
  - [ ] 3.1 Create `deploy/dapr/config.yaml` with secret scoping (NOT access control policy -- that's a separate DAPR mechanism):
    ```yaml
    apiVersion: dapr.io/v1alpha1
    kind: Configuration
    metadata:
      name: memories-config
    spec:
      secrets:
        scopes:
          - storeName: secretstore
            defaultAccess: deny
            allowedSecrets:
              - embedding-api-key
              - llm-secret
    ```
    **Note:** DAPR API token authentication is configured via environment variables (`APP_API_TOKEN`, `DAPR_API_TOKEN`), NOT via the configuration YAML `api.allowed` block. The `api.allowed` block controls DAPR access control policy (which APIs are callable) -- a separate concern. Do not conflate the two mechanisms.
  - [ ] 3.2 Configure DAPR API token in Aspire AppHost (`src/Hexalith.Memories.AppHost/Program.cs`). Add environment variables to the DAPR sidecar and application:
    - Set `APP_API_TOKEN` on the Memories Server project (application validates incoming sidecar-to-app calls)
    - Set `DAPR_API_TOKEN` on the DAPR sidecar (sidecar validates incoming app-to-sidecar calls)
    - Use Aspire parameter or secret for token value (development only)
  - [ ] 3.3 Document the DAPR API token configuration in inline comments. For production: tokens injected via environment variables or Kubernetes secrets. For development: use a fixed dev token via Aspire configuration.
  - [ ] 3.4 **Scope:** This task configures DAPR-level token authentication (sidecar rejects unauthenticated requests). It does NOT implement application-level `TenantAuthorizationMiddleware` (that's Phase 1.5, architecture decision D8). MVP validates that DAPR API tokens are configured and documented.
  - [ ] 3.5 **Testability note:** AC3 is fundamentally untestable in unit tests -- DAPR API token validation is handled by the DAPR runtime, not application code. Validation requires either Aspire integration tests (verify sidecar rejects unauthenticated requests) or manual verification. Document this gap explicitly.

- [ ] Task 4: FalkorDB query scoping audit (AC: #4)
  - [ ] 4.1 Audit all FalkorDB query paths to confirm tenant database scoping. All callers must use `tenantId` as the FalkorDB graph/database name:
    - `IndexGraphActivity.RunAsync` -- uses `input.TenantId` as graph ID
    - `GraphScopedSearch.ExecuteAsync` -- uses `normalizedQuery.TenantId` as graph ID
    - `CaseService` graph operations -- uses tenantId parameter as graph ID
    - `GraphTraversalService` -- uses tenantId parameter as graph ID
    - `VerifyConsistencyActivity` -- uses tenantId as graph ID
    - `CleanupGraphActivity` -- uses tenantId as graph ID
    - `DeleteFalkorDbBatchActivity` -- uses tenantId as graph ID
  - [ ] 4.2 Confirm all Cypher queries go through `IGraphQueryBuilder` / `GraphQueryBuilder` -- no raw Cypher string construction. Grep for `GRAPH.QUERY` calls and verify they use builder-generated queries only. Known exception: `TenantIsolationVerifier` uses `GRAPH.LIST` and `GRAPH.QUERY` directly for infrastructure-level isolation testing (not application data queries) -- this is acceptable per D9.
  - [ ] 4.3 If any violations are found, fix them. If all paths are already compliant, document the audit results in a comment block in the story file's Dev Agent Record section.
  - [ ] 4.4 Add a code comment in `GraphQueryBuilder.cs` header referencing this audit and D9 compliance: `// All Cypher queries must go through this builder (D9). Audited in Story 5.4.`

- [ ] Task 5: Unit tests for tenant context enforcement (AC: #1, #2)
  - [ ] 5.1 `tests/Hexalith.Memories.Server.Tests/Tenants/TenantContextEnforcementTests.cs`
  - [ ] 5.2 Test cases for AC1 (registry validation):
    - Embedding config endpoint rejects unknown tenant with `TENANT_NOT_FOUND`
    - Embedding config endpoint rejects non-Active tenant (Provisioning, Deleting, Failed)
    - Provision status endpoint rejects unknown tenant
    - Deletion status endpoint allows Deleting tenant (existence-only check)
    - Verify endpoint allows non-Active tenant (existence-only check)
    - Verify endpoint rejects unknown tenant
  - [ ] 5.3 Test cases for AC2 (mismatch detection):
    - CaseService returns null when memory unit TenantId mismatches requested tenant
    - CaseService logs Critical when tenant mismatch detected (isolation breach indicator)
    - Endpoint returns 404 (not data leakage) when memory unit belongs to different tenant
  - [ ] 5.4 Test cases for AC4 (graph scoping):
    - GraphScopedSearch uses tenantId as FalkorDB database name (verify via mock)
    - **Note:** "GraphQueryBuilder produces parameterized queries" is an audit finding (Task 4), not a unit test -- you cannot unit test the absence of string interpolation. Verify via code review in Task 4.
  - [ ] 5.5 **AC3 is not unit-testable.** DAPR API token validation is handled by the DAPR runtime. See Task 6 for integration-level verification.

- [ ] Task 6: Integration tests for tenant context enforcement (AC: #1, #2, #3)
  - [ ] 6.1 `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantContextEnforcementIntegrationTests.cs`
  - [ ] 6.2 Test cases:
    - Request with unknown tenantId returns 409 with `TENANT_NOT_FOUND` on data endpoints
    - Request targeting tenant A's case from tenant B's context returns 404 (not data leakage)
    - DAPR sidecar rejects requests without API token (if configurable in test environment)
    - Search scoped to tenant A returns zero results from tenant B's indexes
  - [ ] 6.3 Integration tests may use `[Fact(Skip = "Requires Aspire AppHost fixture")]` consistent with 5-1, 5-2, 5-3 deferral pattern. Required before Gate 2 sign-off.

## Dev Notes

### Dependencies

- **Story 5-1 (Tenant Provisioning):** Required -- provides `TenantRegistryService`, `TenantStatusGuard`, `TenantIdGuard`, `IndexSchemaDefinitions`, all tenant contracts. Status: done.
- **Story 5-2 (Tenant Deletion):** Provides `TenantStatusGuard` with existing status check logic. Status: done.
- **Story 5-3 (Tenant Isolation Verification):** Independent but complementary. Story 5-3 verifies **data isolation** (no cross-tenant data in indexes). Story 5-4 verifies **endpoint-level enforcement** (rejecting cross-tenant API requests). Status: review.

### Implementation Priority

Implement in this order to satisfy ACs incrementally:
1. **AC1 first:** Systematic registry validation (Task 1) -- lowest risk, highest impact, closes the biggest gap
2. **AC2 second:** Mismatch detection (Task 2) -- defense-in-depth, complements AC1
3. **AC4 third:** FalkorDB audit (Task 4) -- likely already compliant, confirms rather than changes
4. **AC3 last:** DAPR API token configuration (Task 3) -- infrastructure configuration, not code logic

### Architecture Compliance

- **NFR8 (Hard Gate):** Zero cross-tenant data leakage. This story enforces it at the API boundary layer, complementing Story 5-3's data-level verification.
- **NFR10:** All inter-service communication authenticated via DAPR API tokens. This story configures it.
- **FR44:** System enforces tenant context at all access layers, rejecting cross-tenant requests with clear error messages.
- **D8 (TenantAuthorizationMiddleware):** Phase 1.5, NOT part of this story. MVP validates tenant IDs against the registry. Phase 1.5 maps authenticated identity -> authorized tenant set.
- **D9 (IGraphQueryBuilder):** All Cypher data queries through builder. Audited in Task 4.
- **D1 (FalkorDB isolation):** Database-level isolation. Graph queries scoped by using tenantId as database/graph name.

### Existing Infrastructure to Reuse

| Component | Location | Usage in This Story |
|-----------|----------|---------------------|
| `TenantStatusGuard` | `Server/Tenants/TenantStatusGuard.cs` | `ValidateTenantActiveAsync()` -- add to unprotected endpoints |
| `TenantRegistryService` | `Server/Tenants/TenantRegistryService.cs` | `GetTenantAsync()` for existence-only checks |
| `TenantIdGuard` | `Server/Activities/Indexing/TenantIdGuard.cs` | Already used via `ValidateTenantId()` helper -- no changes needed |
| `ValidateTenantId()` | `Program.cs` (static helper, line ~1487) | Format validation -- already called on all endpoints. Keep as-is. |
| `ErrorResponse` | `Contracts/V1/ErrorResponse.cs` | Standard error response format |
| `IGraphQueryBuilder` | `Server/Graph/IGraphQueryBuilder.cs` | Parameterized Cypher queries -- audit only, no changes expected |
| `GraphQueryBuilder` | `Server/Graph/GraphQueryBuilder.cs` | Implementation -- audit for raw Cypher violations |
| `CaseService` | `Server/Cases/CaseService.cs` | Add tenant mismatch checks in data retrieval methods |

### Endpoint Audit Summary (Current State)

**Fully protected** (format + registry + status check):
- `POST /api/ingest` -- `ValidateIngestionRequest` + `TenantStatusGuard`
- `GET /api/search` -- `ValidateTenantId` + `TenantStatusGuard`
- `POST /api/tenants/{tenantId}/cases` -- `CaseValidator.ValidateTenantId` + `TenantStatusGuard`
- `GET /api/tenants/{tenantId}/cases` -- `ValidateTenantId` + `TenantStatusGuard`
- `DELETE /api/tenants/{tenantId}/cases/{caseId}` -- `CaseValidator` + `TenantStatusGuard`
- `POST /api/tenants/{tenantId}/cases/{caseId}/members` -- `CaseValidator` + `TenantStatusGuard`
- `DELETE /api/tenants/{tenantId}/cases/{caseId}/members/{memberId}` -- `CaseValidator` + `TenantStatusGuard`
- `DELETE /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` -- `CaseValidator` + `TenantStatusGuard`

**Partially protected** (format check only, NO registry check -- **must fix**):
- `GET /api/tenants/{tenantId}/embedding-config` -- `ValidateTenantId` only
- `PUT /api/tenants/{tenantId}/embedding-config` -- `ValidateTenantId` only
- `POST /api/tenants/{tenantId}/provision-status/{instanceId}` -- `ValidateTenantId` only
- `GET /api/tenants/{tenantId}/deletion-status/{instanceId}` -- `ValidateTenantId` only
- `POST /api/tenants/{tenantId}/verify` -- `ValidateTenantId` + `TenantIdGuard` only

**Administrative** (no tenant context needed):
- `POST /api/tenants` -- Creates new tenant (no existing tenant to validate)
- `GET /api/tenants` -- Lists all tenants
- `GET /api/tenants/{tenantId}` -- Get tenant info (already checks registry, returns 404)
- `DELETE /api/tenants/{tenantId}` -- Tenant deletion (already checks registry)

### Error Codes (Updated)

Existing tenant error codes:
- `TENANT_NOT_FOUND` (404) -- tenant does not exist in registry
- `TENANT_ALREADY_EXISTS` (409) -- duplicate provisioning attempt
- `TENANT_DELETING` (409) -- tenant is being deleted
- `TENANT_PROVISIONING` (409) -- tenant is still provisioning
- `TENANT_FAILED` (409) -- tenant in failed state
- `TENANT_UNAVAILABLE` (409) -- tenant in unknown non-active state

New error code for this story:
- `TENANT_MISMATCH` (403) -- data belongs to a different tenant than the request context. **This is a critical security event -- log at Critical level.** Should never occur if physical isolation is working correctly.

### Code Conventions

- Sealed partial class for services using `[LoggerMessage]` source generator
- File-scoped namespaces: `namespace Hexalith.Memories.Server.Tenants;`
- `ErrorResponse("CODE", "message", "suggestion")` pattern for all error responses
- Singleton DI registration for stateless services
- xUnit + Shouldly + NSubstitute for testing
- Test naming: `{ClassName}Tests.cs` with descriptive method names
- Keyed DI: `"redis"` for RediSearch/Vector, `"falkordb"` for FalkorDB

### Anti-Patterns to Avoid

1. **Do NOT implement `TenantAuthorizationMiddleware`** -- that's Phase 1.5 (D8). This story adds registry validation, not identity-based authorization.
2. **Do NOT add ASP.NET Core middleware** for tenant validation. The current codebase uses minimal API delegates with explicit validation. Adding middleware would change the architecture pattern. Stick with explicit `TenantStatusGuard` calls in each endpoint.
3. **Do NOT modify `TenantIdGuard`** -- it validates format only, which is its correct responsibility. Registry validation is `TenantStatusGuard`'s job.
4. **Do NOT duplicate validation logic** -- use `TenantStatusGuard.ValidateTenantActiveAsync()` consistently. Do NOT create a new validation helper.
5. **Do NOT modify `IGraphQueryBuilder` or `GraphQueryBuilder`** -- AC4 is an audit, not a rewrite. Only fix if actual violations are found.
6. **Do NOT add ambient tenant context (e.g., `AsyncLocal<string>`, `ITenantContext`)** -- the codebase uses explicit parameter-based multi-tenancy. This is a deliberate design choice (easier to audit, no implicit state).
7. **Do NOT use `ValidateTenantId()` as a substitute for `TenantStatusGuard.ValidateTenantActiveAsync()`** -- `ValidateTenantId()` only checks format. The whole point of AC1 is that the tenant must exist in the registry.

### Previous Story Learnings (from 5-3)

- FalkorDB `RedisServerException` for graph-not-found must be caught gracefully
- ETag-based CAS retry loop (max 3 retries) used for concurrent DAPR state updates
- `DaprException` wrapping -- catch and return 503 `DAPR_UNAVAILABLE` for sidecar issues
- Resilience pattern: catch `RedisConnectionException` / `RedisServerException` internally, return structured errors
- Endpoint pattern: top-level try-catch for `DaprException` -> 503, `RedisException` -> 503
- `Stopwatch.Elapsed.TotalMilliseconds` (not `.Milliseconds`) for timing
- 1051+ tests currently passing -- run full suite before and after to catch regressions

### Git Intelligence

Recent commits show:
- `acbcffe` -- Unit tests for tenant deletion activities and workflows (5-2)
- `5bb2655` -- Unit tests for tenant provisioning activities and workflows (5-1)
- All prior Epic 5 stories are complete with test coverage established

### Edge Cases

- **Provisioning-in-progress tenant:** `GET /api/tenants/{tenantId}/provision-status/{instanceId}` must work for Provisioning tenants (that's the whole point of the endpoint). Use existence-only check, not active-status check.
- **Deleting tenant:** `GET /api/tenants/{tenantId}/deletion-status/{instanceId}` must work for Deleting tenants. Same approach.
- **Verify endpoint:** `POST /api/tenants/{tenantId}/verify` should work for any existing tenant regardless of status (useful for diagnosing Failed tenants).
- **Tenant mismatch on physical isolation:** If a `TENANT_MISMATCH` is ever detected at the data layer, this is a **critical infrastructure bug**, not a user error. Log it at `Critical` level with full context (requested tenantId, actual tenantId, resource type, resource ID).

### Gate 2 Sign-off Criteria (Story 5.4)

Gate 2 sign-off for this story requires ALL of the following:
1. All unit tests for tenant context enforcement pass (Task 5) -- registry validation, mismatch detection, graph scoping
2. All integration tests pass on real infrastructure -- not deferred, not skipped (Task 6)
3. Endpoint audit confirms ALL tenant-scoped endpoints validate against the tenant registry (Task 1 + Task 4)
4. DAPR API token is configured and verified on a running Aspire AppHost instance (Task 3) -- manual verification acceptable since AC3 is not unit-testable
5. No `TENANT_MISMATCH` errors detected during normal operation (confirms physical isolation is intact)

### Known MVP Limitations

- **No identity-based authorization:** MVP validates tenant IDs against the registry but does not map authenticated identities to tenant sets. Any caller that provides a valid tenant ID can access that tenant's data. `TenantAuthorizationMiddleware` (D8) addresses this in Phase 1.5.
- **DAPR API token is not per-tenant:** The API token authenticates the sidecar-to-application channel, not individual tenant access. It prevents external parties from directly calling the DAPR sidecar but does not provide tenant-level access control.
- **No audit trail for tenant access:** Access telemetry is structured logging only in MVP (search activity logging exists in `CaseActivityService`). Dedicated audit store is Phase 2.

### Project Structure Notes

New files go in:
- `deploy/dapr/config.yaml` -- DAPR configuration with API token and secret scopes
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantContextEnforcementTests.cs` -- unit tests
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantContextEnforcementIntegrationTests.cs` -- integration tests

Modified files:
- `src/Hexalith.Memories.Server/Program.cs` -- add `TenantStatusGuard` calls to unprotected endpoints
- `src/Hexalith.Memories.Server/Cases/CaseService.cs` -- add tenant mismatch checks
- `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs` -- audit comment only
- `src/Hexalith.Memories.AppHost/Program.cs` -- DAPR API token configuration

### References

- [Source: _bmad-output/planning-artifacts/epics.md -- Epic 5, Story 5.4]
- [Source: _bmad-output/planning-artifacts/architecture.md -- NFR8, NFR10, D1, D8, D9, FR44]
- [Source: _bmad-output/planning-artifacts/prd.md -- FR44, NFR10]
- [Source: _bmad-output/implementation-artifacts/5-3-tenant-isolation-verification.md -- Previous story patterns]
- [Source: src/Hexalith.Memories.Server/Program.cs -- Endpoint audit baseline]
- [Source: src/Hexalith.Memories.Server/Tenants/TenantStatusGuard.cs -- Existing validation]
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/TenantIdGuard.cs -- Format validation]
- [Source: src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs -- D9 contract]
- [Source: src/Hexalith.Memories.Server/Cases/CaseService.cs -- Data access layer]
- [Source: deploy/dapr/components/statestore.yaml -- Current DAPR config baseline]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
