---
title: 'Preserve tenant isolation diagnostics during backend failures'
type: 'bugfix'
created: '2026-11-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve DW-695, DW-699, DW-700, DW-701 and DW-708 as one reliability change to the read-only tenant isolation verifier.

- Given a tenant embedding configuration lookup that stays pending, when the verifier's lookup deadline elapses, then the semantic check reports configuration unavailable and verification continues through the independent checks.
- Given a Redis or FalkorDB command timeout, when verification reaches the affected check, then it records failed backend-unavailable evidence and continues with independent checks.
- Given a non-hash key within the requested syntactic prefix, when the hash read returns WRONGTYPE, then the check identifies the affected key and still examines the rest of the prefix.
- Given cancellation before or during a verification read, when the caller's token is canceled, then the verification propagates cancellation and stops issuing subsequent reads.
- Given marker or classification evidence from the raw semantic scan, when the natural-language scan fails, then the failed semantic result retains that evidence and tells the operator the scan is incomplete.

Preserve read-only operation, tenant scoping and the V1 check result shape. Verify the existing cross-tenant negative probes and the new failure cases before pushing to main.

</frozen-after-approval>

## Implementation Notes

- Baseline `ac14212bd81e52b7834927e5071de52c288d40d1`. The root and its ten direct reference repositories were synchronized with their remote main branches before planning. The reference checkouts can stay current independently of the dependency revisions tracked by this story's source commit.
- Map: `TenantIsolationVerifier.cs` owns the configuration lookup, five check boundaries and both cursor scans. `TenantIsolationVerifierTests.cs` has the reusable dependency, prefix and cancellation fixtures. Close the five selected records in the deferred-work ledger with focused evidence.
- A five-second configuration deadline keeps a stalled read bounded. The verifier returns its existing unavailable result on timeout, preserves caller cancellation with the original token, and observes a detached lookup through completion. For Redis commands, cancellation stops waiting and prevents subsequent requests. Commands already dispatched are read-only.
- Debug build of the server test project passed with zero warnings and errors. The 81-case focused verifier run passed after review. The baseline build and 68-case verifier run also passed.

- Full Server.Tests with the CI-pinned kubectl v1.35.0 reported 3,335 passed, one failed and one existing skip. `OperationalRunbookSetTests.RequiredRunbooks_ExistAndHaveExactSharedHeadings` failed on an extra capacity-planning heading. The same test failed with the documented heading restored from baseline `ac14212bd81e52b7834927e5071de52c288d40d1`.
- `ASPIRE_HOME=/tmp/memories-deferred-aspire-home aspire run --apphost src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` built the AppHost and failed when its startup path attempted to write to the read-only `/home/agent/.aspire`. Live AppHost validation was unavailable.
- The independent reviewer confirmed the seven review fixes.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs` - verifier failure handling
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs` - read-only failure and cancellation regressions
- `_bmad-output/implementation-artifacts/deferred-work.md` - evidence for the five resolved entries
- `_bmad-output/implementation-artifacts/spec-five-deferred-tenant-isolation-reliability.md` - scope, review and verification

## Cross-Tenant Negative Evidence

**Surfaces:** Syntactic and semantic tenant marker diagnostics and tenant-scoped backend reads. Story 24.3 establishes the tenant marker boundary.

**Tests:** `TenantIsolationVerifierTests.VerifyAsync_SyntacticWrongType_ReportsKeyAndContinuesCheckingThePrefix`, `TenantIsolationVerifierTests.VerifyAsync_NaturalLanguageScanTimeout_RetainsAllPreviouslyCollectedEvidence`, and `TenantIsolationVerifierTests.VerifyAsync_ForeignActiveMarkerWithExtraPayloadFields_DoesNotExposePayloadContent`.

**Command:** `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net11.0/Hexalith.Memories.Server.Tests.dll -class '*TenantIsolationVerifierTests'`

**Result:** 81 passed. The verifier reported the named foreign marker after a failed hash read or a second-prefix timeout. The mocks confirmed tenant-scoped reads and no storage mutations.

## Review Triage Log

- Medium, patched. Redis connection, server and timeout failures use fixed diagnostic labels. The disconnected-backlog case verifies that driver details stay out of the check result.
- Medium, patched. Timed-out and canceled lookups are observed through completion. The concrete actor provider's ability to cancel its sidecar request is tracked in DW-681.
- Medium, patched. A non-hash key gets guidance to inspect the reported key and identify its writer. A wrong-type-only test exercises this outcome independently of marker mismatch.
- Medium, patched. Both scans accumulate evidence as each key is verified. The natural-language timeout test observes a foreign marker before the interruption and checks both prefixes' findings.
- Medium, patched. Injected a syntactic cursor timeout, a hash-read timeout and a timeout in the initial graph-list read. Independent checks still pass.
- Low, patched. The pre-cancellation test now checks zero Dapr registry calls, zero provider calls and zero Redis/FalkorDB calls.
- Medium, patched. Only a leading WRONGTYPE reply code identifies a non-hash key. An ordinary server error mentioning the same word reports backend unavailability.
