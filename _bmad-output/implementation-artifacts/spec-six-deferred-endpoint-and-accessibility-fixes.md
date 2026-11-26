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

## Cross-Tenant Negative Evidence

**Surfaces:** Ingestion body tenant selection and audit context. The ingestion requests preserve the [Story 20.2 authorization boundary](20-2-tenant-authorization-filter-and-principal-derived-audit-identity.md).
**Tests:** `ServerEndpointAuthorizationTests.TenantScopedIngestSchedulingEndpoint_WithMismatchedBodyTenant_ReturnsTenantForbiddenBeforeSchedulingDependencies`.
**Command:** `dotnet tests/Hexalith.Memories.Server.Tests/bin/Release/net11.0/Hexalith.Memories.Server.Tests.dll -class "*IngestionEndpointE2ETests" -class "*ServerEndpointAuthorizationTests"`.
**Result:** The body tenant mismatch cases passed for all three routes. Each returned 403 before tenant storage and scheduling dependencies.

## Review Triage Log

- DW-528, patched. The audit scope retains the rejected-tenant tag and tests verify its error events. Validation checks for a missing URL request before reading fetcher options.
- DW-528, outside the reported wire shape. A chunked body with no JSON bytes follows the framework’s malformed-JSON path.
