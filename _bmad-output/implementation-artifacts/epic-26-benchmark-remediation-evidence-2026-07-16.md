# Epic 26 Benchmark Remediation Evidence

**Date:** 2026-07-16  
**Baseline:** `56faf29454be613a09ca3865b7ba3c9844dc5f9b`  
**Relevant implementation revisions:** `9c6bbb24`, `4598ff4e`

The relevant changes were included in two concurrent `/pushall` commits that also contained unrelated workspace work. This record scopes the benchmark remediation to the production and test files listed below.

## Production rationale

The syntactic and semantic benchmark axes return top-10 candidate lists. With RRF `k=60`, normalized rank 10 contributes `61/70 = 0.8714` of rank 1, so weak cross-axis occurrences can displace strong top-ranked evidence. Production RRF now uses `k=10`, where rank 10 contributes `11/20 = 0.55` of rank 1. Live syntactic/semantic/graph defaults are calibrated from `0.40/0.40/0.20` to `0.30/0.35/0.35`; NL remains `0.20` and default-off. Legacy `StoredFusionWeights` missing-field fallbacks remain unchanged.

Relevant files:

- `src/Hexalith.Memories.Contracts/V1/FusionWeights.cs`
- `src/Hexalith.Memories.Server/Search/FusionEngine.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/FusionWeightsSerializationTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Search/FusionEngineTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Serialization/PersistenceCompatibilityTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Search/ExplainSearchApiIntegrationTests.cs`

`git diff --exit-code 56faf294..4598ff4e -- tests/Hexalith.Memories.Benchmarks` passed with no differences. The corpus, ground truth, query set, NDCG@10 scorer, top-10 cutoff, strict comparison, 80% threshold, reproducibility test, and 17-test inventory are unchanged.

## Verification

| Lane | Command | Result |
|---|---|---|
| Contracts | `dotnet test tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj -c Release` | 588 passed, 0 failed, 0 skipped |
| Server | `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Release` | 2,670 passed, 0 failed, 1 established skip |
| Explain integration | `dotnet test tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~ExplainSearchApiIntegrationTests"` | 8 passed, 0 failed, 0 skipped |
| Solution build | `dotnet build Hexalith.Memories.slnx -c Release --no-restore` | 0 warnings, 0 errors |
| Benchmark run 1 | `bash tools/test.sh --filter "Category=Benchmark" --configuration Release --no-build --results-directory TestResults/benchmark-run1` | 17 passed, 0 failed, 0 skipped; 8/8 wins |
| Benchmark run 2 | `bash tools/test.sh --filter "Category=Benchmark" --configuration Release --no-build --results-directory TestResults/benchmark-run2` | 17 passed, 0 failed, 0 skipped; 8/8 wins |

The complete per-query result payloads from run 1 and run 2 matched exactly. Both runs reported `hybridWins=8`, `hybridWinRate=1.0`, and `thesisValidated=true`. TRX evidence is retained under `TestResults/benchmark-run1/Hexalith.Memories.Benchmarks/` and `TestResults/benchmark-run2/Hexalith.Memories.Benchmarks/`; generated result JSON remains ignored build output.

## Per-query strict comparison

| Query | Hybrid NDCG@10 | Best single-axis NDCG@10 | Margin | Win |
|---|---:|---:|---:|---|
| BQ-01 | 0.888752 | 0.756825 | +0.131927 | Yes |
| BQ-02 | 0.975788 | 0.812268 | +0.163520 | Yes |
| BQ-03 | 0.839194 | 0.802038 | +0.037156 | Yes |
| BQ-04 | 0.972665 | 0.853115 | +0.119550 | Yes |
| BQ-05 | 0.881495 | 0.775281 | +0.106214 | Yes |
| BQ-06 | 0.857348 | 0.836370 | +0.020977 | Yes |
| BQ-07 | 0.892211 | 0.866241 | +0.025970 | Yes |
| BQ-08 | 0.993078 | 0.866241 | +0.126838 | Yes |

## Gate decision

The unchanged PRD benchmark gate passes at 8/8 strict wins, exceeding the required 7/8. Reproducibility and test-inventory gates pass, so the Epic 26 benchmark action and the story/retrospective alignment blocker are resolved.
