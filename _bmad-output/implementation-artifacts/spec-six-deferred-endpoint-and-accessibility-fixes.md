---
title: 'Complete six deferred API and accessibility fixes'
type: 'bugfix'
created: '2026-11-26'
status: 'in-progress'
route: 'oneshot'
baseline_commit: '1f68406be7a61064044963139941c9ba2c117947'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete the next six actionable deferred-work entries in register order. Return structured errors for missing ingestion bodies, invalid edge types, and tenant workflow outages. Preserve the deletion scheduling error if rollback fails. Split cohesive slices from the largest Server endpoint and registration files. Verify the populated benchmark comparator with Chromium and axe.

</frozen-after-approval>

## Implementation Notes

- DW-528 makes the three ingestion bodies optional at binding, records validation errors in the existing telemetry scope, and rejects missing bodies before tenant storage or scheduling.
- DW-528 review retained the rejected-tenant trace tag and verified audit events. Empty chunked JSON remains subject to the framework’s malformed-JSON handling.
- Tenant isolation evidence for ingestion body routing. The Release Server test assembly ran `-class "*IngestionEndpointE2ETests" -class "*ServerEndpointAuthorizationTests"` in the 71-case focused run. `TenantScopedIngestSchedulingEndpoint_WithMismatchedBodyTenant_ReturnsTenantForbiddenBeforeSchedulingDependencies` passed for all three endpoints and verified denial before dependencies.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-six-deferred-endpoint-and-accessibility-fixes.md` - implementation and verification
- `_bmad-output/implementation-artifacts/deferred-work.md` - dispositions
- `src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs` - nullable request bodies
- `src/Hexalith.Memories.Server/Endpoints/EndpointTelemetryHelpers.cs` - missing body audit fields
- `tests/Hexalith.Memories.Server.Tests/Endpoints/IngestionEndpointE2ETests.cs` - body and audit regressions
- `src/Hexalith.Memories.Server/Endpoints/GraphEndpoints.cs` - DW-531 implementation or verification
- `tests/Hexalith.Memories.Server.Tests/Endpoints/EdgeConfidenceEndpointTests.cs` - DW-531 implementation or verification
- `src/Hexalith.Memories.Server/Endpoints/TenantLifecycleEndpoints.cs` - DW-532 implementation or verification
- `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantLifecycleEndpointTests.cs` - DW-532 implementation or verification
- `src/Hexalith.Memories.Server/Endpoints/TenantLifecycleEndpoints.cs` - DW-533 implementation or verification
- `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantLifecycleEndpointTests.cs` - DW-533 implementation or verification

## Cross-Tenant Negative Evidence

**Surfaces:** Ingestion body tenant selection and audit context. The ingestion requests preserve the [Story 20.2 authorization boundary](20-2-tenant-authorization-filter-and-principal-derived-audit-identity.md). Edge-confidence PATCH validation before graph selection. Provision and deletion status routing and workflow ID ownership. Tenant deletion claim ownership and rollback.
**Tests:** `ServerEndpointAuthorizationTests.TenantScopedIngestSchedulingEndpoint_WithMismatchedBodyTenant_ReturnsTenantForbiddenBeforeSchedulingDependencies`. `EdgeConfidenceEndpointTests.Patch_InvalidEdgeTypeOrUnauthorizedTenant_StopsBeforeGraphAccess`. `TenantLifecycleEndpointTests.GetStatus_UnauthorizedTenant_RejectsBeforeRegistryOrWorkflowLookup` and `TenantLifecycleEndpointTests.GetStatus_WrongWorkflowTenant_RejectsBeforeWorkflowLookup`. `TenantLifecycleEndpointTests.Delete_UnauthorizedTenant_RejectsBeforeRegistryOrWorkflowLookup` and `TenantRegistryServiceTests.UpdateTenantStatusAsync_StaleRollbackAgainstNewerDeletingOwner_IsBlocked`.
**Command:** `dotnet tests/Hexalith.Memories.Server.Tests/bin/Release/net11.0/Hexalith.Memories.Server.Tests.dll -class "*IngestionEndpointE2ETests" -class "*ServerEndpointAuthorizationTests"`. The same assembly with `-class "*EdgeConfidenceEndpointTests"`. The same assembly with `-class "*TenantLifecycleEndpointTests"`. The same assembly with `-class "*TenantLifecycleEndpointTests" -class "*TenantRegistryServiceTests"`.
**Result:** The body tenant mismatch cases passed for all three routes. Each returned 403 before tenant storage and scheduling dependencies. All five graph cases passed, including the 403 response before graph access. All 16 lifecycle cases passed. Wrong workflow tenants, including overlapping tenant names, were denied before workflow access. All 55 lifecycle and registry cases passed, including denial before dependencies and refusal to overwrite a newer deletion owner.

## Review Triage Log

- DW-528, patched. The audit scope retains the rejected-tenant tag and tests verify its error events. Validation checks for a missing URL request before reading fetcher options.
- DW-528, outside the reported wire shape. A chunked body with no JSON bytes follows the framework’s malformed-JSON path.
- DW-531, patched. The edge validation checks case-insensitive property matches for compound values that deserialize to another edge. HTTP coverage rejects those inputs and confirms cross-tenant denial. Existing successful promotions are covered by ConfidencePromotionIntegrationTests. Malformed JSON keeps the established error response.
- DW-532, patched. A five-second dependency budget covers registry and workflow reads, including the SDK’s wait-for-ready behavior. Cancellation, distinct permission failures, readiness guidance, and the deletion inbox retry follow their own paths. The generated workflow ID’s final delimiter separates suffixes from tenant IDs, including tenant names that share a prefix. Dapr’s established result for a missing workflow and untyped backend errors remain outside the sidecar-outage fix.
- DW-533, patched. A failed or stalled best-effort rollback preserves the original 503 and logs only the tenant and exception type. The existing deletion claim is retained when the gRPC schedule acknowledgement is uncertain. The rollback test verifies the persisted row and owner after each outcome.
