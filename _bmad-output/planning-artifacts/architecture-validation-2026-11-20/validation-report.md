# Memories architecture validation

20 November 2026 · Hexalith.Memories

**Revision required before implementation handoff.** The 23 decisions pass structural lint. Seven high-priority findings still allow incompatible implementations or would hand a team an incorrect operating contract. Resolve them before treating the affected decisions as build instructions. The product’s Phase 1 release decision remains no-go.

## Repeat validation · 17:52 CET

Four reviewers rechecked the prior findings on the same architecture revision. All seven high-priority and eight medium-priority findings remain open at their original severity. Structural lint again passed all 23 decisions.

The query cutover rule fences writes but does not bind all reads in one request to a single epoch. The erasure rule still requires a pre-deletion sample even when the target was always empty. Two medium findings have narrower scope: the public rank-one score is already tested at 1.0, but the spine does not distinguish that scale from raw RRF, and offset pagination is ambiguous only when the corpus changes between requests. The package evaluation and seven image-digest checks passed again.

## Scope and evidence

- Validated the working-tree architecture dated 20 November 2026 on branch `work`, repository commit `75c5b5f53af8ce122a42393d5ec7ff795d554df0`. The file includes the uncommitted architecture update.
- Ran the repository’s `lint_spine.py` against the existing run folder. It returned zero findings. All 23 decision IDs are present in order. The alignment ledger contains 55 rows, of which 51 are marked active-foundation critical. Those rows track implementation and evidence obligations separately from the 15 validation findings.
- Checked the release/package selection with MSBuild, matched seven image digests to source manifests, and checked recorded gitlinks against the working submodules. The validation used the refreshed Builds, FrontComposer and Tenants checkouts from the update.
- Reviewed the November PRD and release decision, the decision log, lifecycle and recovery rules, code paths and relevant test sources. No application test suite, live restore, fault-injection run or release qualification was run in this validation.

Reviewed file SHA-256: `e97c37a46cc0d5c557621d4f6cc075a7f22a8ff1f3a752dd6118d10ff93a2c28`. [Architecture spine](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [November release decision](../prd.md#release-decision-record)

## Independent reviews

| Review | Focus |
| --- | --- |
| [Good-spine rubric](reviews/review-rubric.md) | Tenant erasure, Production dependencies, internal service contracts and G6 accounting. |
| [Technology and code reality](reviews/review-technology.md) | Current MemoryUnitId issuance, reservation release and health probes. The checked package versions, seven image digests and recorded gitlinks matched. |
| [Cross-component consistency](reviews/review-divergence.md) | Repair acknowledgements, mixed-epoch reads, public scoring, pagination and canonical evidence bytes. |
| [Data lifecycle and privacy](reviews/review-lifecycle.md) | Empty-target evidence, resumability after key destruction, telemetry custody and retention receipts. |

## High-priority findings

### H1 · A repair can accept acknowledgements from before the loss

**AD-3**

After a projection is lost, the coordinator marks its checkpoint for reprojection. A delayed success from the original work still matches the same source version, schema generation and embedding epoch. All three old acknowledgements could arrive after invalidation and return the unit to Indexed.

**Proposed correction.** Bind acknowledgement to the repair attempt or checkpoint revision. In a regression case, deliver all three old successes after invalidation. The status, case activity and counters must wait for three fresh confirmations.

[Spine lines 115–119](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Cross-component consistency review](reviews/review-divergence.md)

### H2 · One search can combine two embedding epochs

**AD-10 · AD-14 · AD-22**

An adapter reading before an atomic cutover and one reading after it can both truthfully report that their own read used the active pair. The current response contract carries a boolean and does not identify a common pair for the whole query.

**Proposed correction.** Capture one schema generation and embedding epoch at admission. Require each adapter to return the pair it read and define retry or degraded-response behavior when they differ. Exercise a cutover between two axis reads.

[Spine lines 167, 193–195, 278](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Cross-component consistency review](reviews/review-divergence.md)

### H3 · An empty tenant cannot supply every required erasure sample

**AD-16**

Completion requires a known pre-deletion record from every physical target and ciphertext from each applicable backup class. A newly provisioned tenant may never have written content, staged an export or created a backup. There is no specified way to prove an empty target and complete erasure.

**Proposed correction.** Define a fenced, independently verifiable empty or inapplicable outcome. Keep pre-deletion samples for populated targets and fail closed on missing or unreachable evidence. Cover both an unused tenant and one that has never exported.

[Spine lines 207–209](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [FR39](../prd.md#functional-requirements) · [Data lifecycle and privacy review](reviews/review-lifecycle.md) · [Good-spine rubric review](reviews/review-rubric.md)

### H4 · Final erasure can lose its proof after tenant-key destruction

**AD-6 · AD-16 · AD-21**

A crash can occur after the tenant key has been destroyed and before the permanent tombstone is appended. On restart the workflow must prove the same Deleting generation. The surviving lifecycle mirror names the tenant, state and transition time. Its role and contents do not establish the required authoritative generation proof.

**Proposed correction.** Define content-free evidence of the committed Deleting revision and generation that survives key destruction and authorizes only completion of that deletion. Crash at this boundary and verify restart appends exactly one tombstone.

[Spine lines 141–143, 209, 215, 268–272](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Data lifecycle and privacy review](reviews/review-lifecycle.md)

### H5 · Tenant deletion gives telemetry owners conflicting cleanup instructions

**AD-6 · AD-16 · AD-17**

The lifecycle rule and ledger tell the tenant owner to erase the telemetry partition with the tenant. The erasure and retention rules require a durable handoff and retain opaque telemetry until its bounded expiry. Dropping the partition immediately and retaining its contents lead to different outcomes.

**Proposed correction.** State which tenant access and principal mappings end at deletion, who retains the records and erasure mapping, and who removes the partition after the last applicable record expires. Verify that product access ends immediately and retention cleanup can still finish.

[Spine lines 141, 209, 212, 221–225, 472](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Retention controls](../../../docs/operations/access-telemetry-lifecycle.md) · [Data lifecycle and privacy review](reviews/review-lifecycle.md)

### H6 · The ledger offers a second owner for the Production gateway

**AD-1 · AD-2 · AD-19**

The environment contract assigns shared lifecycle to Hexalith.Platform and lists the EventStore gateway as an external dependency. The gap ledger also permits adding an owned Production gateway workload. The separate Memories DomainService command/replay host has a different role.

**Proposed correction.** Make the convergence row identify who supplies the gateway, who configures routing and authentication, and which deployment must prove connectivity. If ownership is to change, record the decision before assigning implementation.

[Spine lines 101, 306, 314, 405, 443, 457](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Route ownership](../../../docs/operations/route-surface.md) · [Good-spine rubric review](reviews/review-rubric.md)

### H7 · The identifier table describes target ULIDs as already issued

**AD-23**

The normative table describes MemoryUnitId as a 26-character uppercase ULID issued by the current component. Native ingestion currently creates GUID-based IDs and may prefix an ownership generation. Enforcing the table immediately would reject identifiers the service still produces.

**Proposed correction.** Label ULID issuance as the target and identify live producer formats. Coordinate producer cutover, the already approved pre-release re-ingestion reset, and validator activation. Verify the supported transition at both write and read boundaries.

[Spine lines 284, 292, 305](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Native ID issuance](../../../src/Hexalith.Memories.Server/Ingestion/IngestionWorkflowConfigurationCapture.cs) · [Generation-qualified IDs](../../../src/Hexalith.Memories.Server/Ingestion/UnitOwnershipAuthority.cs) · [Story 34.36](../epics.md#story-3436-issue-canonical-ulids-for-every-new-case-and-memory-unit) · [Technology and code reality review](reviews/review-technology.md)

## Medium-priority findings

### M1 · Retention receipts have two possible stream destinations

**AD-17 · AD-21**

AD-17 and its ledger place purge completion in the erased-tenant register. AD-21 reserves that stream’s sequence for the population genesis and erasure tombstones. Other platform records have their own write authority.

**Proposed correction.** Name a separate platform stream for retention receipts and verify that a purge receipt leaves the erased-tenant register revision unchanged.

[Spine lines 221, 268–272, 494](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Data lifecycle and privacy review](reviews/review-lifecycle.md)

### M2 · Public per-axis scores have two plausible scales

**AD-9 · AD-12**

AD-9 gives the raw reciprocal-rank contribution as 1/(10 + rank). The implementation exposes a top-rank-normalized per-axis value. A presenter could display 1/11 at rank one while another displays 1.0, even if they agree on the fused score.

**Proposed correction.** Define raw contributions and public per-axis scores separately. Add a rank-one fixture that compares the serialized values across supported surfaces.

[Spine lines 161, 179](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Fusion engine](../../../src/Hexalith.Memories.Server/Search/FusionEngine.cs) · [Score tests](../../../tests/Hexalith.Memories.Server.Tests/Search/FusionEngineTests.cs) · [Cross-component consistency review](reviews/review-divergence.md)

### M3 · The pagination guarantee has no stability boundary

**AD-9**

Consecutive offsets are required to reproduce one ordering, but inserts, deletes or a new publication can change rank between requests. Both a live offset implementation and a snapshot implementation could claim conformance.

**Proposed correction.** Specify the guarantee for fixed inputs or bind pages to a result revision and define expiry. Verify the contract with a source change between page one and page two.

[Spine lines 119, 161](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Search implementation](../../../src/Hexalith.Memories.Server/Search/HybridSearchService.cs) · [Cross-component consistency review](reviews/review-divergence.md)

### M4 · Evidence Packet reason ordering is unspecified

**AD-12**

Canonical output fixes property order and scalar formatting while describing reasons as a set. Two serializers can emit the same reasons in opposite array order and fail the required byte comparison.

**Proposed correction.** Define a canonical order and duplicate-handling rule for reason codes. Verify identical bytes when reasons arrive in different orders.

[Spine line 179](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Cross-component consistency review](reviews/review-divergence.md)

### M5 · The telemetry contract has two publication instructions

**AD-12 · AD-17 · AD-19**

The route map calls telemetry and clock endpoints internal. The ledger offers publishing their contracts as an alternative. That leaves public API ownership and compatibility requirements unresolved.

**Proposed correction.** Apply the intended internal classification consistently, including consumer eligibility, wire-name registration and package inventory. Require an explicit architecture decision before publishing the contract.

[Spine lines 179, 306, 443, 493](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Package inventory](../../../tools/release-packages.json) · [Good-spine rubric review](reviews/review-rubric.md)

### M6 · Four active ledger rows omit required tracker-freeze details

**AD-20 · G6**

The rows for partial restore, CI selection, capacity and the McpCli cold run name work items and review dates. They omit the per-row sprint-status obligation or the applicable freeze reference required by AD-20 and the PRD.

**Proposed correction.** For DW-733, DW-731, DW-735 and Story 35.7, record the tracker obligation, freeze reference and review date together. Keep the evidence requirement explicit.

[Spine lines 260, 452, 459–461](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [G6 evidence rule](../prd.md#measurable-outcomes) · [Good-spine rubric review](reviews/review-rubric.md)

### M7 · The reservation registry promises owner-only release

**AD-4 · AD-8**

After a reservation expires and a new caller acquires the key, a late scheduling failure can delete the new reservation. EventStore preflight and the normal REST release path delete by key. Separating their key prefixes addresses a different issue.

**Proposed correction.** Record the owner-check gap on both paths. Verify that release by an earlier owner leaves the new reservation in place after expiry and reacquisition.

[Spine lines 324, 492](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [EventStore release](../../../src/Hexalith.Memories.EventStore/RedisPreflightDedupStore.cs) · [Native REST release](../../../src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs) · [Technology and code reality review](reviews/review-technology.md)

### M8 · Dapr sidecar health currently participates in process liveness

**FR72 · Health convention**

The architecture assigns process viability to liveness. The Server tags the sidecar check for both liveness and readiness, and Kubernetes probes /alive. A sidecar outage can therefore trigger a restart of a responsive application. The alignment ledger omits this discrepancy.

**Proposed correction.** Record the difference and the governing FR72 behavior. A verification case should keep /alive healthy while the process responds and make /ready fail when the Dapr control boundary is unavailable.

[Spine line 312](../architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md) · [Server registration](../../../src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs) · [Probe routing](../../../src/Hexalith.Memories.ServiceDefaults/Extensions.cs) · [Deployment probe](../../../deploy/kubernetes/base/server-deployment.yaml) · [Probe runbook](../../../docs/dev/health-checks.md#probe-tuning-guidance) · [Technology and code reality review](reviews/review-technology.md)

## Handoff

Address the seven high-priority findings first. Revalidate the corrected decisions against the failure cases above. The release owner still needs the independent evidence recorded for G1–G6 and L1–L3.
