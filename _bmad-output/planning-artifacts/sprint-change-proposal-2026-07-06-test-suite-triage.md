---
change_trigger: "Local full test run requested on 2026-07-06"
mode: batch
status: partially-applied
project: Hexalith.Memories
date: 2026-07-06
scope_classification: moderate
---

# Sprint Change Proposal: Test Suite Triage

Date: 2026-07-06
Project: Hexalith.Memories
Scope: Direct adjustment for deterministic failures, with follow-up decisions for benchmark thesis and integration topology failures.

## 1. Issue Summary

A local `tools/test.sh` run exposed three classes of failures:

- Deterministic governance/test-fixture failures that can be fixed directly:
  - CLI deferred-work baseline tests rejected structured 24.5 entries that had no `Evidence:` or `Rationale:` field.
  - The benchmark fixture smoke test found the expected invoice document but asserted an unstable exact snippet phrase.
  - Delete-projection activity tests expected a scalar Redis semantic-key delete while the implementation now uses the bulk key-delete overload for chunk-aware semantic projection cleanup.
- Benchmark thesis validation still reports 75% hybrid win rate, below the documented 80% Story 2.8 gate.
- Full integration execution still has a topology/provisioning cascade, including Aspire/Dapr readiness timeout symptoms and tenant/index provisioning failures before downstream graph, case, search, and workflow assertions.

## 2. Impact Analysis

Epic 19 / deferred-work governance: The release baseline gate correctly requires actionable evidence for open deferred entries. The 24.5 structured entries were missing evidence text, so the artifact rather than the test was wrong.

Story 2.8 / benchmark validation: The benchmark infrastructure is now stable enough to execute, but the thesis gate remains unmet at 75% (6/8). Existing implementation notes classify this as a product decision point, not a code bug. The 80% threshold should not be silently lowered.

Delete-projection cleanup coverage: The focused server tests now align with current chunk-aware Redis semantic-key deletion behavior and continue to prove failure ordering preserves retry safety.

Integration lane stability: The failures observed after the deterministic fixes are broader than one assertion. They point to environment/topology and provisioning sequencing issues around Dapr sidecar readiness, tenant semantic-index provisioning, and helper null-safety after early setup failures.

## 3. Recommended Approach

Selected immediate approach: Direct Adjustment.

Rationale: The deferred-work schema, benchmark fixture smoke assertion, and delete-projection test expectation are localized defects. Fixing them does not change product scope, architecture, or PRD commitments.

Required follow-up approach: Moderate product/architecture review for the thesis benchmark gate, plus a separate integration stabilization story for the topology/provisioning cascade.

Do not change:

- Do not lower the Story 2.8 80% benchmark gate as part of this fix.
- Do not hide the thesis validation failure behind a broad test filter.
- Do not treat integration cascade failures as resolved because non-integration lanes pass.

## 4. Detailed Change Proposals

Applied deterministic changes:

- Add explicit `Evidence:` fields to all six open 24.5 deferred-work entries.
- Make the benchmark fixture cleanup idempotent after partial initialization failure.
- Keep the benchmark smoke test anchored to the exact invoice document ID while relaxing the snippet assertion to case-insensitive `invoice` content.
- Align delete-projection tests with the Redis bulk semantic-key delete overload used by chunk-aware cleanup.

Deferred decision areas:

- Benchmark thesis gate:
  - Option A: recalibrate fusion weights with documented benchmark evidence.
  - Option B: review ground-truth query expectations and corpus coverage.
  - Option C: revisit whether graph-axis contribution is required before enforcing the 80% thesis gate.
  - Option D: keep the current result as an explicit open product risk until the benchmark set is expanded.
- Integration stabilization:
  - Add a dedicated story for Dapr/Aspire readiness diagnostics, tenant provisioning order, semantic-index availability before performance smoke tests, and helper behavior after setup failures.

## 5. Implementation Handoff

Routed to: Developer agent for direct deterministic fixes; PM, architect, and test architect for thesis-gate and integration-stabilization decisions.

Success criteria for the applied adjustment:

- CLI deferred-work baseline tests pass.
- Server delete-projection activity tests pass.
- Benchmark non-thesis tests pass.
- Fast unit/contract lanes pass.
- `git diff --check` passes.

Success criteria for follow-up work:

- Story 2.8 thesis gate either reaches the documented 80% target through approved calibration/ground-truth/graph-axis changes, or remains explicitly tracked as a product decision.
- Integration-fast/full integration execution completes without `/alive` sidecar timeout cascades, tenant-index-not-provisioned failures, or null-reference follow-on failures from failed setup.
