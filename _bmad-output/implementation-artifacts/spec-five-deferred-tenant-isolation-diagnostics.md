---
title: 'Make tenant isolation diagnostics safer to act on'
type: 'bugfix'
created: '2026-11-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve DW-680, DW-682, DW-683, DW-705 and DW-715 in the read-only tenant isolation verifier.

- Given a missing verifier dependency, when constructed, then the verifier rejects it with the corresponding parameter name.
- Given many semantic findings, when verification finishes or a backend interrupts a scan, then diagnostics report a bounded sample for each finding category and count omitted findings. Verification examines every key it can reach.
- Given multiple registered graph databases, when the requested tenant’s graph exists, then its structural result reports target existence and cites the independent content proof without exposing the database count.
- Given connected Redis primaries and replicas, when the verifier enumerates key prefixes, then it visits the primaries. If no connected primary is available, report the affected checks as unavailable.
- Given syntactic marker defects, metadata defects or non-hash keys, when verification reports repair guidance, then it gives distinct steps for each finding and tells operators to verify provenance before tenant-scoped marker repair.

Preserve the V1 result shape, tenant-scoped prefixes, and read-only operation. Verify the five findings and push the product change to main.

</frozen-after-approval>

## Implementation Notes

- Baseline `3a9ddc78caae3abda059bfb93aacef8dede9dbff`. The requested pull advanced ten direct reference checkouts. Memories product changes can be committed independently of dependency revisions.
- `TenantIsolationVerifier.cs` owns the public diagnostic and both prefix scans. Keep sampled evidence across both semantic prefixes and retain it if the scan is interrupted. A dedicated evidence collector will bound retained samples. Reuse `TenantIsolationVerifierTests.cs` for fixtures and read-only assertions.
- Semantic evidence retains ten samples per category across both prefixes. It counts additional findings without halting the read-only scan.
- A connected replica does not substitute for a primary when enumerating keys. The independent graph checks continue if Redis primary discovery fails.

- Debug builds succeeded with zero warnings and errors using both source references and published packages. The source-reference verifier run passed all 93 tests.
- The published-package Server.Tests run used kubectl v1.35.0 and reported 3,347 passed, one failed and one skipped. `OperationalRunbookSetTests.RequiredRunbooks_ExistAndHaveExactSharedHeadings` failed on the extra capacity-planning heading. Invoking the same method against baseline `3a9ddc78caae3abda059bfb93aacef8dede9dbff` reproduced the failure.
- `ASPIRE_HOME=/tmp/memories-verifier-aspire-home aspire run --apphost src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` built the AppHost and failed to start when Aspire attempted to write to the read-only `/home/agent/.aspire`. The test evidence is from the Server.Tests harness.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` - verifier diagnostics and scan selection
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.SemanticScanEvidence.cs` - semantic evidence sampling and totals
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs` - verifier regressions
- `_bmad-output/implementation-artifacts/deferred-work.md` - dispositions for the five findings
- `_bmad-output/implementation-artifacts/spec-five-deferred-tenant-isolation-diagnostics.md` - intent, review and test evidence

## Cross-Tenant Negative Evidence

**Surfaces:** Tenant-scoped Redis prefix scans, syntactic and semantic marker reporting, and structural graph diagnostics. Story 24.3 establishes the tenant marker boundary.

**Tests:** `TenantIsolationVerifierTests.VerifyAsync_SyntacticMarkerDefect_ReportsNamedKeyAndProvenanceGuidance`, `TenantIsolationVerifierTests.VerifyAsync_LargeSemanticScan_BoundsEachEvidenceCategoryAndKeepsScanning`, `TenantIsolationVerifierTests.VerifyAsync_RedisReplica_DisregardsReplicaKeysAndRequiresConnectedPrimary`, `TenantIsolationVerifierTests.VerifyAsync_GraphIsolation_IsStructuralOnlyAndCitesContentProof`.

**Command:** `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net11.0/Hexalith.Memories.Server.Tests.dll -class '*TenantIsolationVerifierTests'`

**Result:** 93 passed against a Debug source-reference build. The fixtures reported foreign markers only under the requested tenant's prefixes, rejected a replica-only scan, omitted the registered peer from graph evidence, and verified that the checks issued no writes.

## Review Triage Log

- Medium, patched. The successful orphan check repeated the cluster total. Both successful graph results now omit it, and the structural test uses two registered databases.
- Medium, patched. Redis hash reads require primary routing so replica fallback cannot supply marker evidence after discovery. Regression coverage checks command flags and a primary lost during the scan.
- Medium, deferred. Connected primaries alone do not prove complete cluster slot coverage. The ledger records the topology qualification required to diagnose missing shards.
- Medium, deferred. The cap bounds the number of findings. The ledger records a separate display budget for unusually large stored values.
- Medium, deferred. Syntactic verification already returned before the scan when the index was missing. The ledger records the missing-index review path.
- Low, patched. The semantic cap test exercises marker remediation with samples and omitted keys as well as mixed findings.
- Low, patched. The syntactic marker test independently verifies missing and foreign markers, named evidence and read-only behavior.
