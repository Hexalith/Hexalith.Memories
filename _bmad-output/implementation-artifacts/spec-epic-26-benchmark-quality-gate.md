---
title: 'Restore Epic 26 Benchmark Quality Gate'
type: 'bugfix'
created: '2026-07-16'
status: 'in-progress'
baseline_commit: '56faf29454be613a09ca3865b7ba3c9844dc5f9b'
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
- [ ] `FusionEngine.cs`, `FusionWeights.cs` -- apply the approved calibration and document its rank-decay rationale.
- [ ] Focused tests -- pin rank-10 contribution `11/20 = 0.55`, defaults, overrides, legacy persistence, explicit weights, degraded axes, and NL behavior.
- [ ] Benchmark/evidence -- run the unchanged Release suite twice; retain TRX and a source-controlled per-query summary.
- [ ] Epic 26 records -- after green evidence, append closure notes and mark the benchmark action, alignment action, and epic row done.

**Acceptance Criteria:**
- Given the fixed benchmark, when the complete Release suite runs, then exactly 17 tests pass with none failed/skipped, `hybridWins=8`, `hybridWinRate=1.0`, and `thesisValidated=true`.
- Given two independent benchmark runs, when per-query results are compared, then NDCG@10 values and win outcomes are identical.
- Given regression tests, when they run, then persisted weights, optional axes, attribution, score bounds, ties, and NL remain green.
- Given verified closure evidence, when trackers reconcile, then historical 6/8 evidence remains intact.

## Spec Change Log

## Design Notes

Neither change passes alone. Combined, they stay 8/8 for `k=8..16` and nearby weights; minimum margin is `+0.02098`.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj -c Release` -- all pass.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Release` -- all pass except established skips.
- `dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~ExplainSearchApiIntegrationTests"` -- focused integration passes.
- `dotnet build Hexalith.Memories.slnx -c Release` -- expected: zero warnings and errors.
- `bash tools/test.sh --filter "Category=Benchmark" --configuration Release --results-directory TestResults/benchmark` -- expected: 17 passed, 0 failed, 0 skipped; unchanged gate reports 8/8 and reproducibility passes.
