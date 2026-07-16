---
title: 'Restore Epic 26 Benchmark Quality Gate'
type: 'bugfix'
created: '2026-07-16'
status: 'done'
baseline_commit: '56faf29454be613a09ca3865b7ba3c9844dc5f9b'
implementation_revisions: ['9c6bbb24', '4598ff4e']
story: '26.8'
approved_proposal: '../planning-artifacts/sprint-change-proposal-2026-07-16-epic-26-benchmark-closure.md'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The benchmark reaches 6/8 wins because RRF `k=60` compresses top-10 ranks: rank 10 retains 87.1% of rank 1, so weak multi-axis matches displace graph-led evidence in BQ-03/BQ-07. Epic 26 cannot close.

**Approach:** Set RRF `k=10` and default syntactic/semantic/graph weights to `0.30/0.35/0.35`, retaining NL `0.20`. This restores top-rank discrimination and produced 8/8 wins.

## Boundaries & Constraints

**Always:** Use production defaults; preserve deterministic weighted RRF, ties, optional axes, attribution, bounded scores, explicit weights, and NL default-off. Keep the corpus, ground truth, top-10 cutoff, NDCG@10, strict `hybrid > best active single axis`, 80% threshold, Redis/Falkor execution, 17 tests, and reproducibility unchanged. Preserve unrelated work and append historical evidence.

**Ask First:** Any data, query, gate, public schema, architecture-method, benchmark-only, or durable-state change; any change to legacy `StoredFusionWeights` fallbacks; or any result below 8/8, failed reproducibility, or per-query loss with the approved vector.

**Never:** Lower/bypass the gate, count ties, skip tests, encode answers, branch on benchmark/query IDs, edit generated result JSON as source, or restore raw-score averaging.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Default three-axis fusion | Top-10 syntactic, semantic, and graph rankings | `k=10`, defaults `0.30/0.35/0.35`; deterministic rank order | Fail tests if decay/defaults drift |
| Explicit/partial configuration | Provided/omitted query weights | Provided values remain; omissions use live defaults and explain reports them | Existing validation stays fail-closed |
| Legacy or missing axis | Stored JSON; empty backend | Persistence fallback stays stable; only non-empty axes participate | No silent migration/penalty |
| Benchmark gate | Fixed corpus on pinned containers | 8/8 strict wins, 17/17 tests, identical repeated metrics | Any miss blocks tracker closure |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Memories.Server/Search/FusionEngine.cs` -- RRF constant and rank contributions.
- `src/Hexalith.Memories.Contracts/V1/FusionWeights.cs` -- live defaults.
- `tests/Hexalith.Memories.Server.Tests/**`, `tests/Hexalith.Memories.Contracts.Tests/**` -- fusion/default regressions.
- `tests/Hexalith.Memories.IntegrationTests/Search/ExplainSearchApiIntegrationTests.cs` -- exposed live-default proof.
- `tests/Hexalith.Memories.Benchmarks/BenchmarkSuiteTests.cs` -- unchanged gate consumer; summarize results in `epic-26-benchmark-remediation-evidence-2026-07-16.md`.
- `_bmad-output/implementation-artifacts/{sprint-status.yaml,epic-26-retro-2026-07-16.md,spec-clarify-epic-26-closure-status.md}` -- append-only closure reconciliation after green evidence.

## Tasks & Acceptance

**Execution:**
- [x] `FusionEngine.cs`, `FusionWeights.cs` -- apply the approved calibration and document its rank-decay rationale.
- [x] Focused tests -- pin rank-10 contribution `11/20 = 0.55`, defaults, overrides, legacy persistence, explicit weights, degraded axes, and NL behavior.
- [x] Benchmark/evidence -- run the unchanged Release suite twice; retain TRX and a source-controlled per-query summary.
- [x] Epic 26 records -- after green evidence, append closure notes and mark the benchmark action, alignment action, and epic row done.

**Acceptance Criteria:**
- Given the fixed benchmark, when the complete Release suite runs, then exactly 17 tests pass with none failed/skipped, `hybridWins=8`, `hybridWinRate=1.0`, and `thesisValidated=true`.
- Given two independent benchmark runs, when per-query results are compared, then NDCG@10 values and win outcomes are identical.
- Given regression tests, when they run, then persisted weights, optional axes, attribution, score bounds, ties, and NL remain green.
- Given verified closure evidence, when trackers reconcile, then historical 6/8 evidence remains intact.

## Spec Change Log

- 2026-07-16: Linked approved Sprint Change Proposal and canonical Story 26.8; frozen benchmark and reproducibility boundaries remain unchanged.
- 2026-07-16: Independent approval-gate verification passed; Story 26.8 and Epic 26 closure records reconciled to `done`.

## Design Notes

Neither change passes alone. Combined, they stay 8/8 for `k=8..16` and nearby weights; minimum margin is `+0.02098`.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj -c Release` -- all pass.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Release` -- all pass except established skips.
- `dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Search.ExplainSearchApiIntegrationTests` -- focused integration passes.
- `dotnet build Hexalith.Memories.slnx -c Release` -- expected: zero warnings and errors.
- `bash tools/test.sh --filter "Category=Benchmark" --configuration Release --results-directory TestResults/benchmark` -- expected: 17 passed, 0 failed, 0 skipped; unchanged gate reports 8/8 and reproducibility passes.

## Suggested Review Order

**Ranking calibration**

- Tighter RRF decay restores top-rank discrimination across the governed top-ten candidate window.
  [`FusionEngine.cs:17`](../../src/Hexalith.Memories.Server/Search/FusionEngine.cs#L17)

- Calibrated live defaults also survive omitted and partially serialized JSON properties.
  [`FusionWeights.cs:21`](../../src/Hexalith.Memories.Contracts/V1/FusionWeights.cs#L21)

**Compatibility boundaries**

- Architecture pins calibration while separating live behavior from durable legacy fallbacks.
  [`architecture.md:96`](../planning-artifacts/architecture.md#L96)

- Empty legacy payloads retain historical storage defaults without silent migration.
  [`PersistenceCompatibilityTests.cs:104`](../../tests/Hexalith.Memories.Server.Tests/Serialization/PersistenceCompatibilityTests.cs#L104)

- Partial request weights keep explicit values and fill omissions from live defaults.
  [`SearchEndpointContractTests.cs:298`](../../tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs#L298)

- Explain integration proves clients observe the calibrated graph weight.
  [`ExplainSearchApiIntegrationTests.cs:194`](../../tests/Hexalith.Memories.IntegrationTests/Search/ExplainSearchApiIntegrationTests.cs#L194)

**Regression and gate proof**

- Deep-rank assertions pin decay beyond the benchmark's rank-ten boundary.
  [`FusionEngineTests.cs:236`](../../tests/Hexalith.Memories.Server.Tests/Search/FusionEngineTests.cs#L236)

- Serialization tests lock complete, empty, and partial live-default behavior.
  [`FusionWeightsSerializationTests.cs:40`](../../tests/Hexalith.Memories.Contracts.Tests/V1/FusionWeightsSerializationTests.cs#L40)

- Independent processes produce identical normalized payload hashes and eight strict wins.
  [`epic-26-benchmark-remediation-evidence-2026-07-16.md:76`](epic-26-benchmark-remediation-evidence-2026-07-16.md#L76)

- Procedural guidance links canonical evidence without embedding stale outcomes.
  [`README.md:107`](../../tests/README.md#L107)

- Contributor guard prevents known-red benchmark prose from returning.
  [`coverage_contract_test.py:228`](../../tests/tooling/coverage_gate/coverage_contract_test.py#L228)

**Governance closure**

- Canonical Story 26.8 records the unchanged gate and compatibility contract.
  [`epics.md:4684`](../planning-artifacts/epics.md#L4684)

- Epic, story, benchmark, and alignment rows converge on done.
  [`sprint-status.yaml:391`](sprint-status.yaml#L391)

- Retrospective preserves the historical failure before appending verified closure.
  [`epic-26-retro-2026-07-16.md:206`](epic-26-retro-2026-07-16.md#L206)

- Follow-up retains permanent independent-process CI comparison work.
  [`deferred-work.md:2196`](deferred-work.md#L2196)
