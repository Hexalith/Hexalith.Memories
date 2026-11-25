---
title: 'Complete seven deferred scope and recovery items'
type: 'chore'
created: '2026-11-25'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete the next seven eligible items in ledger order. Record blocked items where they occur and commit each implementation with its ledger resolution.

- DW-483 binds Inspect Source to the scoped selection without exposing rank or raw source URLs.
- DW-484 verifies copy and command redaction parity for the web surfaces that expose those actions.
- DW-485 exercises cockpit state changes and announcements in the existing interactive browser specimen.
- DW-494 localizes the remaining invariant evidence labels and formatted values used in grid and lens components.
- DW-508 distinguishes an empty tenant index from an empty query and verifies the first ingestion through the real search path.
- DW-517 keeps a tenant metrics failure from aborting enrichment for the other tenants in the page.
- DW-518 avoids retaining transiently incomplete summaries for the full cache duration.

</frozen-after-approval>

## Implementation Notes

- Earlier open entries retain their existing dispositions. Newly skipped entries carry the observed blocker in the ledger.

- DW-483 covers the selected memory-unit identifier and rejects stale, cross-tenant, and cross-case selections. DW-484 uses the authorization omission as a restriction on command callbacks and the inspector payload.
- The Playwright specimen loads the Blazor and Fluent assets in its Test environment. The keyboard test covers loading, completion, degradation, and recovery live-region state.
- Grid and lens displays use localized resources. Direct calls to the existing public mappers retain their invariant formatting. The timestamp resource retains the existing round-trip format and offset.
- The authorized empty-index recovery reuses the existing Retry contract kind. The URL ingestion proof verifies the persisted memory and queries the same tenant and case before and after ingestion.
- The tenant page preserves the failed tenant's registry identity and marks its enrichment unavailable. Healthy tenants retain their own values.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/spec-seven-deferred-scope-and-recovery.md` - implementation and verification
- `_bmad-output/implementation-artifacts/deferred-work.md` - dispositions
- `src/Hexalith.Memories.Server/Endpoints/TenantLifecycleEndpoints.cs` - tenant listing enrichment
- `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs` - tenant fault isolation
- `src/Hexalith.Memories.Server/Tenants/TenantSummaryCache.cs` - metrics cache eligibility
- `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantConfigurationEndpointTests.cs` - tenant paging and enrichment tests
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantSummaryCacheTests.cs` - cache eligibility and tenant partition tests

## Cross-Tenant Negative Evidence

**Surfaces:** Tenant page and its tenant-keyed enrichment. The fix preserves the [Story 20.2 authorization boundary](20-2-tenant-authorization-filter-and-principal-derived-audit-identity.md) and the [Story 24.3 physical isolation contract](spec-24-3-physical-tenant-isolation-and-verifier-scaling.md).
**Tests:** `BuildTenantSummariesAsync_IsolatesOneTenantMetricFailure` and `TenantList_WithPaging_ReturnsTenantSummaryArrayAndPagingHeaders`.
**Command:** `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net11.0/Hexalith.Memories.Server.Tests.dll -class '*TenantConfigurationEndpointTests'` and the same executable with `-method '*SearchEndpointContractTests.TenantList_WithPaging_ReturnsTenantSummaryArrayAndPagingHeaders'`.
**Result:** All 17 selected tests passed. A tenant with an enrichment failure receives no sibling metric data. The paged endpoint does not load the excluded tenant's registry entry.
