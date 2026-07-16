---
project: memories
date: 2026-07-16
status: approved
change_scope: moderate
recommended_path: direct-adjustment
approval_required_from: Administrator
approved_by: Administrator
approved_on: 2026-07-16
implementation_status: complete
---

# Sprint Change Proposal — Close the Epic 26 Benchmark Gate

## 1. Issue Summary

Epic 26 is intentionally still `in-progress` even though Stories 26.1–26.7 and the retrospective are complete. Its critical closure action requires the deterministic hybrid benchmark to reach the PRD hard line of at least 7 wins across the fixed 8-query corpus, without weakening thesis or reproducibility checks, or to receive an explicit evidence-based product-governance change.

A current-head Release rerun on 2026-07-16 executed all 17 benchmark tests: 16 passed, none skipped, and only `ThesisValidation_HybridOutperforms80Percent` failed. The unchanged benchmark produced 6/8 strict hybrid wins (75%):

| Query | Hybrid NDCG@10 | Best single axis | Margin | Result |
|---|---:|---:|---:|---|
| BQ-03 | 0.696021 | Graph 0.802038 | -0.106017 | Loss |
| BQ-07 | 0.832504 | Graph 0.866241 | -0.033737 | Loss |

All six other queries were strict hybrid wins. The result reproduces the retained July 13 artifact exactly at the metric level, so the blocker is fusion quality rather than run instability.

The recorded calibration investigation identifies excessive top-10 rank compression in reciprocal-rank fusion (RRF) at `k=60`: rank 10 retains `61/70 = 87.1%` of rank 1's contribution. Weak lower-ranked multi-axis evidence can therefore displace strong graph-led evidence. The proposed production calibration changes RRF to `k=10` and the live three-axis defaults from `0.40/0.40/0.20` to `0.30/0.35/0.35`; rank 10 then retains `11/20 = 55%` of rank 1. The investigation records 8/8 wins for the combined calibration, stability across `k=8..16` and nearby weights, and a minimum winning margin of `+0.02098`. Neither the constant change nor the weight change passes alone.

## 2. Impact Analysis

### Epic and story impact

- Epic 26 cannot close in its current state because its hard product gate remains red.
- Stories 26.1–26.7 remain complete; none should be reopened because none owns production fusion calibration.
- Add Story 26.8, **Benchmark Quality Calibration**, as the corrective scope that owns the production change, regression coverage, unchanged benchmark execution, evidence retention, and tracker reconciliation.
- Keep Epic 26, its benchmark action, and the carried epic/story alignment action open until Story 26.8 is complete and the evidence is green.
- No Epic 27 exists and no future epic needs resequencing.

### Artifact impacts

- **PRD:** No change. Preserve the 80% hard line, fixed 5–10 query scale, NDCG@10, strict comparison against the best active single axis, and identical repeated results. The proposal changes the product implementation to meet the thesis; it does not change the thesis to fit the implementation.
- **Architecture:** Update the weighted-RRF decision to record the calibrated rank constant and live defaults, plus the compatibility boundary for persisted legacy weights. The fusion method remains deterministic weighted RRF and remains a pure function.
- **Epics:** Add Story 26.8 with explicit unchanged-gate acceptance criteria.
- **UX:** No user-flow, visual, accessibility, or interaction change. Existing explainability and benchmark-comparison semantics remain intact.
- **Contracts and persistence:** `FusionWeights` live defaults change behavior. Explicitly supplied weights remain authoritative. `StoredFusionWeights` legacy/missing-field fallbacks remain `0.40/0.40/0.20` with NL `0.20`; no stored data is migrated or reinterpreted.
- **Testing/CI:** Keep the corpus, ground truth, top-10 cutoff, scorer, winner rule, 80% threshold, Redis/Falkor execution, 17-test inventory, and reproducibility comparison unchanged. Add focused tests around rank decay, live defaults, explicit overrides, persisted fallbacks, optional axes, ties, attribution, bounded scores, and NL default-off behavior.
- **Operational/release:** No deployment topology or schema migration. This is a production search-behavior correction and must be called out in release notes.

### Scope and delivery impact

- **Classification:** Moderate. The code delta is small, but it changes default production ranking behavior and requires architecture, backlog, tests, evidence, and closure-ledger reconciliation.
- **Estimated effort:** 0.5–1 implementation day plus review and complete validation.
- **Schedule impact:** Epic 26 remains open until the evidence is green; no separate release-date change is proposed.
- **Rollback:** Restore `k=60` and the live `0.40/0.40/0.20` defaults if non-benchmark regressions appear. Do not roll back by changing benchmark data or assertions.

## 3. Path Evaluation

### Option 1 — Direct adjustment (recommended)

Change the production fusion calibration, add regression coverage, and verify it through the unchanged benchmark.

- **Feasibility:** High. The affected seams are isolated and already identified.
- **Evidence:** The mechanism is consistent with the failures, the combined vector is recorded at 8/8, and the current benchmark is deterministic.
- **Effort:** Moderate but bounded.
- **Risk:** Medium. The main risk is overfitting a synthetic corpus or changing default ranking for existing consumers.
- **Mitigation:** Apply the change to production defaults, not benchmark-only code; retain explicit configuration behavior and persisted fallbacks; pin sensitivity and regression evidence; run the full relevant test surface; preserve the benchmark caveat that synthetic vectors validate fusion correctness rather than real-world production relevance.

### Option 2 — Roll back or reduce delivered scope (rejected)

Rollback would not simplify Epic 26 or remove the product thesis. Restoring earlier operational stories would discard completed value while leaving the search-quality gate unresolved.

- **Feasibility:** Low as a closure strategy.
- **Effort:** High relative to benefit.
- **Risk:** High; completed deployment, recovery, CI, and operational-readiness work would be disturbed.

### Option 3 — Product-governance change (not approved by this proposal)

A governance change is legitimate only if evidence demonstrates that the requirement or evaluation method is invalid—for example, reviewer disagreement below the PRD threshold, incorrect ground truth, a biased winner definition, or a benchmark that no longer represents the product thesis. No such evidence is present:

- the fixed benchmark previously demonstrated the thesis;
- current results are reproducible;
- the two losses have a coherent production-calibration mechanism;
- a bounded production calibration is recorded as reaching 8/8 without changing data or gates.

Lowering the threshold, counting ties as wins, altering expected results after seeing outputs, or weakening exact-repeat comparisons would be a gate waiver, not an evidence-based governance correction. If direct adjustment fails its acceptance criteria, return to governance review with reviewer-validity and sensitivity evidence; do not modify the gate during implementation.

## 4. Detailed Change Proposals

### 4.1 Epic plan — add Story 26.8

**Artifact:** `_bmad-output/planning-artifacts/epics.md`

**Old state:** Epic 26 ends with completed Story 26.7 and has no story that owns the benchmark remediation.

**Proposed new text:**

```markdown
### Story 26.8: Benchmark Quality Calibration

As a product-quality owner,
I want production hybrid-fusion defaults calibrated against the governed benchmark,
So that Epic 26 closes by meeting the product thesis without weakening its evidence gates.

**Acceptance Criteria:**

**Given** the fixed eight-query corpus, ground truth, top-10 NDCG scorer, strict
`hybrid > best active single axis` rule, 80% threshold, and Redis/Falkor execution,
**When** production weighted-RRF calibration is applied and the complete Release benchmark runs,
**Then** all 17 tests pass with none skipped,
**And** at least 7 of 8 queries are strict hybrid wins,
**And** the approved calibration target is 8 of 8 wins with no per-query regression.

**Given** two independent complete benchmark runs,
**When** their per-query results are compared,
**Then** NDCG@10 metrics and win outcomes are identical.

**Given** explicit weights, persisted legacy weights, missing or empty axes, ties,
attribution, score bounds, and NL default-off behavior,
**When** focused regression tests run,
**Then** all established compatibility and determinism contracts remain green.

**Given** verified green evidence,
**When** Epic 26 records are reconciled,
**Then** historical 6/8 evidence remains intact,
**And** Story 26.8, the benchmark action, the alignment action, and Epic 26 are marked done.
```

**Rationale:** The correction changes production behavior and needs canonical ownership. Reopening Story 26.4 would conflate truthful CI-lane delivery with the product calibration that the lane correctly reports as failing.

### 4.2 Architecture — pin calibrated defaults

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`

**Old state:** Architecture requires deterministic weighted RRF but does not pin the calibrated rank constant or live three-axis defaults.

**Proposed addition to the hybrid-fusion decision:**

```markdown
**Epic 26 calibration:** Default three-axis fusion uses RRF `k=10` and live
syntactic/semantic/graph weights `0.30/0.35/0.35`; the optional NL default remains
`0.20` and NL remains default-off. The lower constant restores meaningful top-10
rank decay (`rank10/rank1 = 0.55`, versus `0.871` at `k=60`). Explicit request or
tenant configuration remains authoritative. Durable `StoredFusionWeights` fallback
values remain unchanged for backward compatibility. Benchmark data, NDCG@10,
strict-winner semantics, the 80% hard line, and exact-repeat reproducibility are
governance controls and must not be altered as calibration implementation.
```

**Rationale:** This converts an implicit tuning value into an architectural invariant and records the boundary between live defaults and durable compatibility.

### 4.3 Implementation specification

**Artifact:** `_bmad-output/implementation-artifacts/spec-epic-26-benchmark-quality-gate.md`

**Old state:** The specification is `ready-for-dev` but has no approved Sprint Change Proposal linked to the Epic 26 backlog.

**Proposed update after approval:**

- Link this proposal and Story 26.8 in the specification metadata/change log.
- Keep its frozen intent, boundaries, 8/8 target, sensitivity record, code map, and verification commands unchanged.
- Treat any request to alter corpus data, ground truth, scorer, threshold, strict-win rule, test inventory, repeat comparison, public schema, or persisted fallbacks as a new approval boundary.

### 4.4 Sprint tracking

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

**Old state:** Epic 26 is `in-progress`; Stories 26.1–26.7 and the retrospective are `done`; the benchmark action and alignment action remain open/in-progress.

**Proposed state immediately after approval:**

```yaml
epic-26: in-progress
26-8-benchmark-quality-calibration: ready-for-dev
```

The benchmark action remains `open`, and the story/epic alignment action remains `in-progress`.

**Proposed state only after verified green evidence:**

```yaml
epic-26: done
26-8-benchmark-quality-calibration: done
```

At that point, mark the benchmark action `done` with exact 17/17, 8/8, two-run reproducibility, and evidence-artifact references. Mark the alignment action done because story, retrospective, and hard-gate state will then agree.

### 4.5 Closure records

**Artifacts:** Epic 26 retrospective, closure-status clarification, and a new benchmark-remediation evidence record.

- Append the new outcome; do not rewrite or delete the historical 6/8 result.
- Retain both independent TRX files and a source-controlled per-query comparison.
- Record the fixed corpus/scorer hashes or equivalent inventory evidence, product calibration, all focused/full validation results, and the synthetic-vector caveat.

## 5. Implementation Handoff

**Recipients:** Product Owner (backlog/approval), Architect (decision update), Developer (production calibration), Test Architect (independent evidence and gate integrity).

**Execution order after approval:**

1. Add Story 26.8 and its `ready-for-dev` tracker row; link the existing implementation specification.
2. Change `FusionEngine` from `k=60` to `k=10` and change live `FusionWeights` defaults to `0.30/0.35/0.35`, retaining NL `0.20`.
3. Add focused regression tests before changing closure status. Do not alter `StoredFusionWeights` fallbacks.
4. Run focused contract, server, explain-search integration, Release solution-build, and unchanged benchmark gates.
5. Run the full benchmark twice independently and retain exact per-query evidence.
6. Only after all acceptance criteria pass, append closure evidence and reconcile Story 26.8, benchmark action, alignment action, and Epic 26 to `done`.

**Stop conditions:** Stop and return for governance review if the approved vector produces fewer than 8/8 wins, any repeat differs, any established regression fails, or passing requires a benchmark/data/gate/persistence change.

## 6. Approval Decision

**Recommendation:** Approve Option 1, Direct Adjustment. Do not approve a governance change on the current evidence.

Approval authorizes the Story 26.8 planning/tracker additions and the implementation/verification sequence above. It does not authorize weakening or replacing any benchmark or reproducibility control.

**Decision:** Administrator explicitly approved the direct adjustment on 2026-07-16. Planning, implementation, verification, and evidence reconciliation described by this proposal are authorized; benchmark and reproducibility controls remain frozen.

## Workflow Execution Log

| Date | Event | Result |
|---|---|---|
| 2026-07-16 | Trigger confirmed from Epic 26 retrospective and sprint action ledger | Complete |
| 2026-07-16 | PRD, epics, architecture, UX, story, specification, source, tests, and tracker reviewed | Complete |
| 2026-07-16 | Current-head unchanged Release benchmark rerun | 16 passed, 1 failed, 0 skipped; 6/8 strict hybrid wins |
| 2026-07-16 | Direct Adjustment evaluated | Recommended; recorded calibration target 8/8 |
| 2026-07-16 | Rollback evaluated | Rejected |
| 2026-07-16 | Product-governance change evaluated | Not supported by current evidence |
| 2026-07-16 | Proposal generated | Complete |
| 2026-07-16 | Administrator decision | Direct Adjustment explicitly approved |
| 2026-07-16 | Focused compatibility verification | 7/7 contract, 94/94 server, and 8/8 integration checks passed |
| 2026-07-16 | Two unchanged approval-gate benchmark runs | Each passed 17/17 with 8/8 strict hybrid wins; result payloads identical except timestamp |
| 2026-07-16 | Release solution build | Passed with 0 warnings and 0 errors |
| 2026-07-16 | Story 26.8 and Epic 26 closure reconciliation | Complete |

## Checklist Record

### 1. Understand the trigger and context

- [x] 1.1 Trigger identified: Epic 26 cannot close at 6/8 against its ≥7/8 hard line.
- [x] 1.2 Core problem defined: deterministic production fusion quality, not test execution or reproducibility.
- [x] 1.3 Evidence collected from canonical artifacts, current code, retained JSON, and a current-head rerun.

### 2. Epic impact assessment

- [x] 2.1 Epic 26 cannot complete as currently tracked; corrective Story 26.8 is required.
- [x] 2.2 Epic-level change defined without reopening completed stories.
- [x] 2.3 Remaining/future epic impact reviewed; no Epic 27 or sequencing dependency exists.
- [x] 2.4 No epic is invalidated and no new epic is required.
- [x] 2.5 Priority is Critical and remains before Epic 26 closure or a subsequent release decision.

### 3. Artifact conflict and impact analysis

- [x] 3.1 PRD reviewed; its hard line and evidence method remain unchanged.
- [x] 3.2 Architecture reviewed; calibration details require an additive decision update.
- [N/A] 3.3 UX requires no change.
- [x] 3.4 Contracts, persistence, testing, CI, release notes, evidence, and trackers assessed.

### 4. Path forward evaluation

- [x] 4.1 Direct Adjustment is viable; moderate effort and medium controlled risk.
- [x] 4.2 Rollback/reduced scope is not a viable closure strategy.
- [x] 4.3 PRD/governance review is not justified by current evidence and remains a stop-path if calibration fails.
- [x] 4.4 Direct Adjustment selected as the recommended path.

### 5. Sprint Change Proposal components

- [x] 5.1 Issue summary and current evidence completed.
- [x] 5.2 Epic, artifact, technical, and delivery impacts documented.
- [x] 5.3 Recommended path and alternatives documented.
- [x] 5.4 Detailed old/new proposals and sequence documented.
- [x] 5.5 Moderate-scope cross-functional handoff documented.

### 6. Final review and handoff

- [x] 6.1 Applicable checklist analysis completed.
- [x] 6.2 Proposal checked against repository evidence and current-head benchmark output.
- [x] 6.3 Explicit Administrator approval received on 2026-07-16.
- [x] 6.4 Sprint-status and backlog reconciliation authorized; final closure remains evidence-gated.
- [x] 6.5 Handoff recipients, success criteria, stop conditions, and closure sequence defined.
