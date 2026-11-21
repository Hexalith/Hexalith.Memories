# Architecture rubric review for the November 20 update

**Reviewed revision** `89106ca99d87f6f708bbaa0939c20279a9b1d1352f84bf78918281dce038f6a2`, Memories commit `75c5b5f5`.

**Verdict at this revision.** All 15 findings have a governing architectural decision and an implementation or evidence obligation. One editorial correction to AD-21 is needed for an unambiguous storage handoff. Structural lint passed with zero findings. All 23 decision IDs match the committed spine in order. [Reviewed spine](../ARCHITECTURE-SPINE.md) · [Validation input and finding IDs](../../../architecture-validation-2026-11-20/validation-report.md)

## Decision checks

| Finding | Contract verified in the merged spine | Engineering evidence still owed |
| --- | --- | --- |
| H1 | AD-3 uses a durable attempt revision and source version. Invalidation clears all three acknowledgements. Target fences and coordinator CAS reject work from prior attempts. | Deliver old successes, failures and writes during repair and worker restart. Check persisted state, case activity and counters. |
| H2 | AD-10/14/22 bind an entire authorized query to one concrete generation/epoch lease. Each adapter reports the pair it read. Retirement waits for readers and egress stops upon loss of authority. | Cut over between two reads. Exercise a mismatched pair, cancellation, drain and prevented egress after revocation. |
| H3 | AD-16 admits repeatable fenced proof of a genuinely empty target or an inapplicable backup class. A populated target requires the prescribed sample and verification. | Delete an unused tenant and a tenant with no export. Verify an unknown or unreachable class leaves the deletion pending. |
| H4 | A content-free preparation bound to population, tenant, committed Deleting revision and generation survives key destruction. The authenticated workflow reconciles receipts before one compare-and-append tombstone. | Interrupt destruction, restart, verify the exact key outcome, and show that a lost acknowledgement or competing worker cannot produce a second tombstone. |
| H5 | AD-6 ends ordinary access and purges the principal-token mapping. Platform Operations accepts the retention handoff and eventually removes its verified-empty partition and locator under AD-17. | Verify immediate denial, preservation of existing expiries and cleanup by the restricted principal through interruption. |
| H6 | AD-1 assigns the external gateway, operational route and readiness to Platform. Memories owns its DomainService artifact, handlers, declarations and qualification against that deployment. The ledger names one deployment responsibility. | Demonstrate authenticated command/replay and denial for the three routed domains. [Route contract](../../../../../docs/operations/route-surface.md) |
| H7 | AD-23 labels canonical uppercase ULIDs as the target. The seed and ledger describe current GUID/ULID and generation-qualified writers. Producer cutover, re-ingestion and stricter validation are sequenced. | Exercise all writers, retry/replay, generation fencing, pre-reset read handling and post-cutover canonical issuance. |
| M1 | AD-17/21 assign retention receipts to the retention-purge stream. Only genesis and terminal erasures advance the erased-tenant register. | Prove idempotent receipts leave the register revision unchanged. See the adjacent storage wording finding below. |
| M2 | AD-9 specifies raw RRF for fusion and normalized per-axis values for hybrid evidence. Rank one produces 1/11 internally and 1.0 publicly. | Check exact wire values across supported surfaces. |
| M3 | V1 offset queries read current data. Concatenation requires identical inputs and each request acquires its own lease. | Exercise stable pages and a changed publication or corpus at a page boundary. |
| M4 | AD-12 requires ordinal deduplication and sorting of reason codes before canonical serialization. | Compare extracted bytes for duplicated reasons arriving in opposite orders. |
| M5 | AD-12 limits telemetry/clock consumers to authorized internal workloads and operations. It reserves V1 names and maps errors while AD-19 keeps the projects non-packable. External exposure requires a subsequent decision and publication change. | Add wire registration and error-map conformance, and demonstrate denied external access. [Release inventory](../../../../../tools/release-packages.json) |
| M6 | Each of DW-733, DW-731, DW-735 and Story 35.7 retains a blocker, a responsible owner, the November 20 review, registration owed in `sprint-status.yaml` and the specific freeze references. | Produce the live recovery drill, coverage accounting, capacity measurements and enrolled cold run respectively. Register each item when the freeze lifts. [Freeze authority](../../../sprint-change-proposal-2026-09-12.md#non-negotiable-boundaries) · [November disposition](../../../sprint-change-proposal-2026-11-20.md#artifact-edits-and-dependencies) |
| M7 | REST and EventStore preflight require fresh owner tokens and atomic compare-and-delete. The registry and ledger identify today's key-only deletion. | Exercise expiry and reacquisition on both paths. A stale release must preserve the new owner. |
| M8 | FR72 binds process viability to liveness, control dependencies to readiness, and query-axis loss to degradation. The ledger identifies current sidecar tags and the Deployment's liveness probe. | With a responsive service and failed sidecar, check 200 liveness, 503 readiness, no restart and readiness recovery. |

## Remaining wording finding

**AD-21, platform record placement.** The sentence “It also holds the content-free export-bundle index and AD-6 lifecycle-state projection” immediately follows the description of the retention-purge stream. This leaves it unclear whether those two families belong to that stream or to the broader platform partition. The subsequent writer rules already assign these records to the export and lifecycle operations. Name the partition explicitly and place those records outside the three enumerated streams so implementers preserve all three writer boundaries.

## Rubric and release disposition

The brownfield check covered the current PRD and decision log, declared route owners, release package inventory, reservation delete paths, score calculation and the liveness probe. The separate technology review and validation record provide the dated package and image verification. The stack, environment ownership, data authority, consistency, privacy, recovery and operator boundaries are explicit. The deferred items retain revisit conditions. The feature spine is the architectural root and retains its ratified constraints. [Decision log](../.memlog.md) · [Technology review](review-version-2026-11-20.md) · [PRD release decision](../../../prd.md#release-decision-record)

The architectural corrections define the implementation handoff. Phase 1 remains no-go and Phase 1.5 has no qualifying verdict. This review ran architecture lint and inspected the cited records. The outcome supplies no runtime, isolation, recovery or release-gate evidence.

## Final handoff retest

**Pass.** Verified the merged spine at SHA-256 `3c7bf368360f37c4d77d15ed9e9ee402660e767bbb9fbe85203f813215999f0e`. AD-21 explicitly places the export-bundle index and lifecycle mirror in their own streams on the platform partition. Each record family has one destination and an identified writer.

The retest also confirmed that a lost coordinator checkpoint closes dispatch until continuity is proven or authoritative replay establishes a fresh pair. Permanent retention receipts carry an independent handoff ID and omit reusable locators. A never-provisioned telemetry target can receive a generation-bound no-record receipt on affirmative inventory evidence. These corrections preserve the H1, H3–H5 and M1 dispositions. All 23 IDs remain in order and lint again returned zero findings. All 15 design findings are closed for handoff. The engineering evidence and release gates listed above remain outstanding.
