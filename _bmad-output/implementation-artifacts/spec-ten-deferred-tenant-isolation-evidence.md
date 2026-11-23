---
title: 'Resolve ten deferred tenant isolation evidence findings'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Keep tenant isolation findings reliable and bounded from Redis reads through authenticated graph proof and operator guidance.

- Given an active or unclassifiable semantic hash, when the verifier reads it, then one Redis field batch supplies the discriminators and field-presence signal, including for a present empty description (DW-702 and DW-710).
- Given oversized stored keys or tenant markers, when the verifier emits evidence, then each rendered value has a UTF-8 byte budget and preserves a stable identifier for operator correlation. Given a missing syntactic index and accessible surviving keys, then the verified evidence remains available (two legacy entries).
- Given malformed or out-of-range traversal depth, when a caller targets an unauthorized tenant, then the API denies access before dependencies run (DW-668). The live collision proof also denies a tenant A bearer access to tenant B's graph and returns no graph content (legacy entry).
- Given graph collision verification, when an independently planted node result or stored edge marker appears, then the proof catches it. The edge test restores its fixture state even on assertion failure (DW-673 and DW-685). Empty or null graph listings fail closed, and graph seeding awaits in-flight mutations before honoring cancellation (DW-686).
- Given an operator following the route contract, when they need to reproduce the graph proof, then they reach the canonical commands in the tenant runbook (DW-652).

</frozen-after-approval>

## Implementation Notes

- Baseline `960e1c12df2cf9df7c1262bfdf361d194555fc69` has 316 open numbered records and 142 open legacy records. DW-702 and DW-710 record the same underlying Redis read. The missing-index fix already exists and will be verified before recording closure.
- The baseline Aspire check reported Memories healthy. The EventStore gateway exited 134 in that standalone startup. The AppHost stopped and both temporary data volumes were removed.

- The Redis script returns discriminator fields and description presence in one request. An empty description counts as present, and its text never transfers to the verifier. Oversized and non-UTF-8 identifiers retain a UTF-8 display prefix and a digest of the original Redis bytes.
- Debug source builds for both test projects succeeded in a disposable clone with zero warnings and zero errors. The final four-class Server selection passed 164 cases. The final five-method live Aspire selection passed all five cases. Both test selections had zero failures and skips.
- Closed DW-652, DW-668, DW-673, DW-685, DW-686, DW-702, DW-710 and the three legacy entries recorded as DW-736 through DW-738. BMAD reports 309 open numbered and 139 open legacy entries.

## Cross-Tenant Negative Evidence

**Surfaces:** Tenant-scoped syntactic and semantic Redis verification, and the authenticated graph traversal route.

**Tests:** `TenantIsolationIntegrationTests.VerifyTenant_PlantedActiveSemanticMarkerDefects_ReturnsDistinctNonDestructiveDiagnostics`, `TenantIsolationIntegrationTests.VerifyTenant_PlantedCrossTenantData_DetectsLeakage`, `TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes`, and `TenantIsolationIntegrationTests.VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage`.

**Command:** `dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Debug/net11.0/Hexalith.Memories.IntegrationTests.dll -method '*.TenantIsolationIntegrationTests.VerifyTenant_PlantedActiveSemanticMarkerDefects_ReturnsDistinctNonDestructiveDiagnostics' -method '*.TenantIsolationIntegrationTests.VerifyTenant_PlantedCrossTenantData_DetectsLeakage' -method '*.TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes' -method '*.TenantIsolationIntegrationTests.VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage' -method '*.TenantIsolationIntegrationTests.VerifyTenant_WithTwoProvisionedTenants_CoreIsolationChecksShouldPass'`

**Result:** All five cases passed against the disposable Aspire topology and its real Redis and FalkorDB data stores. A foreign or missing marker was reported in the requested tenant prefix, including a foreign natural-language marker with an empty description field. Binary key evidence retained the original key digest. The other tenant's graph denied the cross-tenant bearer, and the fixture verified its restored edge state.

## Review Triage Log

- Medium, patched. The Redis script uses HEXISTS on the server and returns the presence bit.
- Medium, patched. Scan deduplication uses Redis keys and digests hash original bytes. The live test verifies a binary key's digest.
- Low, patched. Samples are formatted after admission, so an omitted key does not cause abbreviation guidance.
- Medium, patched. Non-UTF-8 keys and stored memory-unit identifiers yield a classification gap.
- Medium, patched. Node controls exercise replacement and mixed markers independently for both fields.
- Medium, patched. The live denial requires the exact JSON error members and checks decoded values for foreign markers.
- Medium, patched. The graph mutation has a server budget below the fixture's Redis timeout and awaits completion before honoring test cancellation. A transport failure still fails the fixture.
- Low, patched. The finally block checks both restored edge properties.
- Low, patched. The graph-list test supplies an actual null array rather than a null scalar.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-ten-deferred-tenant-isolation-evidence.md`
- `docs/operations/route-surface.md`
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.IsolationScanEvidence.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs`
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs`
