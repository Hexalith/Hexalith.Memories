---
title: 'Report concurrent tenant isolation findings'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Operators need tenant-scoped, read-only verification that identifies all applicable recovery steps when several findings coincide.

- Given a proven-active semantic hash with a blank tenant marker, when verification reads it, then it reports incomplete evidence and names the key for provenance review (DW-640).
- Given an I/O or gRPC failure during embedding configuration lookup, when verification runs, then the semantic check reports configuration unavailability and independent checks continue. Caller cancellation still propagates (DW-703).
- Given a classification gap and inconsistent vector dimensions, when the semantic scan completes, then remediation includes both key-family guidance and the configured dimension for reindexing (DW-704).
- Given a classification gap and active missing or foreign markers, when the semantic scan completes, then remediation also names sampled marker keys and calls for provenance review (DW-714).
- Given a missing syntactic index, when Redis can enumerate the tenant prefix, then verification checks surviving keys and reports any marker or type findings before recommending provisioning. A failed scan must preserve the known missing index.

</frozen-after-approval>

## Implementation Notes

- Memories baseline `e5f0b3a66c10115ac7c3c951385ee19ed10e171d`. All ten direct reference repositories were refreshed before implementation.
- Extend the existing verifier and its public-entry-point tests. Preserve Redis primary routing, sampling limits, public contracts and the separate classification for unknown semantic key shapes.

- `TenantIsolationVerifier.cs` now collects syntactic findings as each key is read and combines semantic guidance from the completed scan. `TenantIsolationVerifierTests.cs` exercises these changes through `VerifyAsync`.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` — verifier behavior
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.SemanticScanEvidence.cs` — previous location of the evidence collector
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.IsolationScanEvidence.cs` — shared bounded evidence collector
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs` — regression coverage
- `_bmad-output/implementation-artifacts/deferred-work.md` — finding dispositions
- `_bmad-output/implementation-artifacts/spec-five-deferred-isolation-findings.md` — intent and evidence

## Cross-Tenant Negative Evidence

**Surfaces:** Tenant-scoped Redis prefix scans and diagnostics returned by the V1 isolation verifier.

**Tests:** `TenantIsolationVerifierTests.VerifyAsync_MissingSyntacticIndex_RetainsSurvivingKeyFindings`, `TenantIsolationVerifierTests.VerifyAsync_ClassificationGapAndActiveMarkerDefect_PreservesAllRemediation`, and `TenantIsolationVerifierTests.VerifyAsync_ActiveFamilyMarkerEvidence_MissingAndForeignFailClosed`.

**Command:** `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net11.0/Hexalith.Memories.Server.Tests.dll -class '*TenantIsolationVerifierTests'`

**Result:** 105 verifier tests passed with source references at the refreshed revisions and with package references at the recorded dependency revisions. Both Debug builds reported zero warnings and errors. The tests found the foreign marker under tenant A's prefix, retained the named finding after a later timeout, and checked the Redis command inventory for read-only operations.

## Review Triage Log

- Medium, patched. An interrupted syntactic scan retains both the writer or marker guidance and the connectivity retry. The regression interrupts a read after observing a foreign marker and a non-hash key.
- Medium, patched. Semantic guidance calls for classification and provenance review, including omitted findings, before index repair. Tests check the order and the bounded sample.
- Medium, patched. The semantic missing-index result also directs operators to review surviving syntactic keys before provisioning. A regression removes both indexes.
- Low, patched. A completed syntactic scan explicitly reports zero surviving keys when the index is missing.
- Low, patched. The missing-index fixture asserts the non-hash key and writer investigation on both complete and partial scans.
- Medium, patched. Syntactic findings share the semantic sample limit of ten per category. A 36-key test verifies all reads, three omission counts and bounded output.

The ledger also identifies DW-709 as the duplicate entry for DW-704 and DW-714.

## Verification

The complete Server.Tests assembly on the recorded dependencies ran 3,361 cases with kubectl v1.35.0. It reported 3,359 passed, one skipped and one failed. The failing method, `OperationalRunbookSetTests.RequiredRunbooks_ExistAndHaveExactSharedHeadings`, also failed on the untouched baseline because `docs/operations/capacity-planning.md` includes an extra heading. Aspire CLI 17.0.0 could not start the AppHost because its first-use sentinel requires a writable `/home/agent/.aspire`.
