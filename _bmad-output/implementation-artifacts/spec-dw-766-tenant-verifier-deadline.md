---
title: 'DW-766 tenant verifier actor deadline'
status: 'done'
---

# DW-766 Tenant Verifier Actor Deadline

The verifier applies its five-second lookup budget to the typed Dapr actor call. An isolated Dapr 1.19.1 run observed cancellation on the actor invocation and its outbound state request. A fresh follow-up read supplied the cached value. The three affected test classes passed 176 tests.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/CancellationTrackingActorHandler.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/TenantEmbeddingConfigProviderTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs`
- `_bmad-output/implementation-artifacts/spec-dw-766-tenant-verifier-deadline.md`

## Cross-Tenant Negative Evidence

**Surfaces:** Tenant-scoped semantic verifier and its embedding configuration lookup. Story 24.3 covers tenant marker verification.
**Tests:** `TenantIsolationVerifierTests.VerifyAsync_ForeignActiveMarker_ReturnsPossibleContaminationWithNamedKey`, `TenantIsolationVerifierTests.VerifyAsync_ForeignActiveMarkerWithExtraPayloadFields_DoesNotExposePayloadContent`, `TenantIsolationVerifierTests.VerifyAsync_EmbeddingConfigLookupDeadline_AbortsTheTypedActorRequest`.
**Command:** Build `tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj` in Release and run its xUnit assembly with a `-method` selector for each of the three fully qualified tests above.
**Result:** All three passed. The verifier flagged the foreign tenant marker without mutation and omitted the foreign payload. Lookup cancellation reached the typed actor request and left the four independent checks able to run.
