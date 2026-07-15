---
baseline_commit: 1ce41926feca03bdd4bbe74e053db45535980421
---

# Story 26.4: Coverage Gating & Benchmark Lane

Status: done

<!-- Epic 26 — Test, Deployment & Operational Readiness. CI/tooling-only story closing audit finding A42 plus the explicitly assigned Epic 25.8 PR-package carry-forward. Do not change retrieval behavior, benchmark data, package versions, product code, or submodule pointers to make a gate pass. -->

## Story

As a test architect,
I want a coverage gate and a benchmark CI lane,
so that regressions in the remediation epics are caught.

## Context

The July audit premise is directionally correct but its live anchors have moved. The repository now has local coverage switches in `tools/test.sh` and `tools/test.ps1`, Coverlet on every test project, a Docker-free unit/contract inventory, a benchmark inventory, and a daily `.github/workflows/nightly.yml`. CI still does not enable coverage or enforce a threshold; the checked-in runsettings still contains the blanket `**/Program.cs` exclusion; and the nightly workflow runs only integration jobs, not the NDCG suite.

There are three additional live defects/carry-forwards that the short epic entry does not expose:

1. `tests/tests.runsettings` is currently invalid XML because an XML comment contains `--coverage`. The documented/scripted coverage command fails before collection with `An XML comment cannot contain '--'`.
2. The latest locally generated Release benchmark result in this diagnostic workspace reports 6 hybrid wins from 8 queries (75%) and `thesisValidated: false`, below the PRD's hard 80% line. That `bin/**/benchmark-results.json` file is gitignored and will not exist in a fresh checkout; implementation must reproduce or supersede it by running the suite. The scheduled lane must expose the result honestly. This story must not tune fusion, edit the corpus/ground truth, filter out `ThesisValidation_HybridOutperforms80Percent`, or use `continue-on-error` to manufacture a green lane.
3. Epic 25.8's package/topology validation still runs only after merge in `release.yml`: PR CI never packs and validates the actual `.nupkg` set, and it discovers neither `tests/tooling/release_packages/` nor `tests/tooling/publish_nuget/`. The Epic 25 retrospective explicitly assigns that PR-lane gap to Story 26.4.

Story 26.3 supplies the meaningful failure-mode baseline for this gate. It is in review with 20 implemented proofs, 8 structured accepted skips, a complete lane of 243 passed/8 skipped/0 failed, and a CI-fast lane of 224 passed/8 skipped/0 failed. The stale `bmad-dev-auto-result-26-4-coverage-gating-and-benchmark-lane.md` blocker about a missing previous-story continuity decision is therefore resolved.

## Acceptance Criteria

1. **Coverage collection is a real blocking PR/push gate.** The existing `test-unit-contract` job in `.github/workflows/ci.yml` runs the existing Docker-free inventory through `tools/test.sh` with coverage enabled, using Release outputs and `tests/tests.runsettings`. Test failure, collector failure, invalid settings, zero executed tests, validator failure, or coverage below the configured minimum fails the job. Coverage stays Docker-free; Story 26.4 does not pull Aspire/Testcontainers into this gate.

2. **The metric, scope, merge, and threshold are explicit and reproducible.** The gate enforces unioned line coverage of **at least 78.0%** over first-party production source emitted for the covered `src/Hexalith.Memories.*` assemblies. It merges overlapping Cobertura reports by normalized repository-relative source path and line number, taking the maximum hit count; it never adds report-root counters. It excludes test assemblies, `Hexalith.Memories.TestHelpers`, `Hexalith.Memories.Web.Specimens`, external/submodule assemblies such as FrontComposer, generated `obj/**`, and migration/tool assemblies outside the declared source scope. A version-controlled configuration is the single source of truth for the threshold and required production assemblies. Missing reports, malformed XML, no in-scope executable lines, or a missing required assembly/source scope fails closed. The gate emits total covered/valid lines, percentage, and per-assembly diagnostics.

3. **Composition roots are no longer hidden.** `tests/tests.runsettings` is valid XML and no longer excludes `**/Program.cs` or any equivalent blanket composition-root pattern. The coverage tooling/contract tests prove that the Server, CLI, and MCP `Program.cs` files appear with executable line data in the merged result; a later exclusion or disappearance fails the gate. This story does not claim runtime coverage for AppHost/Aspire projects absent from the Docker-free test dependency graph, but it must not exclude them by filename to inflate the percentage.

4. **Coverage evidence is retained and the gate itself is guarded.** CI uploads the per-project TRX and Cobertura files on success or failure with the repository's 14-day test-artifact convention. Validator tests cover pass, below-threshold, zero-report, malformed-report, duplicate-report, out-of-scope dependency, missing-required-assembly, Windows/POSIX path normalization, and missing-composition-root cases. Workflow/runsettings contract checks prevent removal of coverage collection, the validator, threshold configuration, or the benchmark lane. The normal developer commands and CI behavior are documented in `tests/README.md`; the stale claim that no root CI workflow exists is removed.

5. **The existing nightly workflow runs the real NDCG project.** `.github/workflows/nightly.yml` gains a bounded benchmark job under its existing `workflow_dispatch` and daily schedule. It restores/builds with the pinned SDK and calls `tools/test.sh --filter "Category=Benchmark"` so `tools/test-projects.benchmark.txt` remains the inventory source. For this exact selector, both test wrappers use the selector to choose the benchmark inventory but clear the effective `dotnet test` trait filter, causing all **17** tests in `Hexalith.Memories.Benchmarks` to run (10 scorer tests, 4 seeder tests, and 3 Testcontainers suite tests). The current implementation forwards `Category=Benchmark` and therefore runs only 13/17, silently omitting the untraited seeder tests. The job executes a nonzero count against the pinned Redis Stack and FalkorDB images, does not duplicate benchmark orchestration in YAML, and does not run solution-level `dotnet test`.

6. **Benchmark quality and reproducibility remain blocking, with durable evidence.** The nightly job runs all existing benchmark tests, including the 80% hybrid-win thesis test and the two-run identical-NDCG@10 reproducibility test. No trait filter, assertion, corpus, score, or `continue-on-error` change hides a failure. The job uploads TRX plus the exact `benchmark-results.json` written under the Release output even when tests fail; missing TRX or JSON is an artifact error, not a warning. A truthful job that executes 17 tests, retains evidence, and fails at the current 75% thesis assertion satisfies this story's automation scope and may transition Story 26.4 to `done`; it must never be described as a green quality result. The linked open Epic 26 readiness action remains the separately owned blocker to completing Epic 26 until retrieval quality reaches the PRD hard line or product governance explicitly changes that line.

7. **Epic 25's package/topology gate moves before merge.** The already-required `test-unit-contract` PR job runs the existing `tests/tooling/release_packages/*_test.py` and `tests/tooling/publish_nuget/*_test.py` suites, then calls an inventory-driven package-only mode added to `tools/pack-release.ps1` with a synthetic stable SemVer and clean CI output directory. That mode retains the script's existing real `.nupkg` validation through `tools/validate-release-packages.ps1 -PackageDirectory <that-directory> -Version <that-version>` but stops before container/deployment preparation. Inventory/schema drift, an unexpected/missing/duplicate package, ProjectReference topology regression, wrong dependency version/range, or fixture failure blocks the protected check. Reuse `tools/release-packages.json` and the existing release orchestration; do not publish, invoke semantic-release, require secrets, or prepare containers/deployment artifacts in this PR gate.

8. **The implementation reuses the pinned test platform and existing seams.** Keep .NET SDK 10.0.301, VSTest (`TestingPlatformDotnetTestSupport=False`), `coverlet.collector` 10.0.1, `Microsoft.NET.Test.Sdk` 18.7.0, xUnit v3 3.2.2, and `xunit.runner.visualstudio` 3.1.5. Do not add `coverlet.MTP`, `coverlet.msbuild`, ReportGenerator, BenchmarkDotNet, inline `.csproj` versions, or a second test runner in this story. A future Microsoft.Testing.Platform migration is separate and must replace collector/runsettings semantics deliberately rather than mixing integrations.

9. **Verification is complete and scope-honest.** The new tooling tests and both release-package fixture suites pass; `Hexalith.Memories.slnx` builds in Release with zero warnings/errors; the Docker-free coverage command passes all expected tests and the 78.0% validator; coverage artifacts contain the required composition roots; CI packs and validates the nine approved `.nupkg` files with the synthetic version; workflow contract tests pass; and `git diff --check` is clean. If Docker is available, execute the benchmark command and retain its TRX/JSON; the expected whole-project baseline is 16 passed/1 failed at the known 75% thesis assertion, reported as pre-existing evidence rather than “fixed” in this story. Changes remain limited to CI/nightly workflows, coverage and package-gate configuration/validation/tests, shared test/release tooling needed by those lanes, and contributor/test documentation.

## Tasks / Subtasks

- [x] **Task 1 — Freeze the live baseline and gate contract** (AC: 2, 3, 6, 7, 9)
  - [x] Re-run the audit preflight at implementation start. Record the revision, package/test-platform properties, current coverage reports, current benchmark JSON, and any moved anchors.
  - [x] Preserve a version-controlled coverage configuration (for example `tools/coverage-thresholds.json`) containing the 78.0% minimum, first-party source scope, and required assemblies. CI, local validation, and tests must consume this source rather than repeat literals.
  - [x] Record the diagnostic baseline: 4,374 passed/1 skipped in the six Docker-free projects; 25,990/33,114 first-party lines = 78.49% after source-line union with `Program.cs` restored. Do not treat raw per-report or all-dependency rates as the gate.
  - [x] If present, record the local diagnostic baseline from `tests/Hexalith.Memories.Benchmarks/bin/Release/net10.0/benchmark-results.json`: 6/8 wins, 75%, `thesisValidated=false`. The file is gitignored and non-authoritative; on a fresh checkout, regenerate it with the Docker benchmark command rather than treating its absence as source drift. Never edit generated `bin/**` evidence as source.

- [x] **Task 2 — Repair and narrow the existing Coverlet configuration** (AC: 1, 2, 3, 8)
  - [x] Make `tests/tests.runsettings` well-formed XML (XML comments cannot contain `--`) and add a fixture/contract test that parses it.
  - [x] Remove `**/Program.cs` from `ExcludeByFile`; retain only justified generated-code exclusions such as `**/obj/**` and the existing attribute exclusions unless a measured reason requires change.
  - [x] Add explicit Coverlet include/exclude filters for repo-owned production assemblies versus tests, test helpers/specimens, and external dependencies. Keep the VSTest `XPlat Code Coverage` collector and Cobertura format.
  - [x] Do not change test project package references: every actual test project already references `coverlet.collector` with the correct private assets.

- [x] **Task 3 — Implement a fail-closed union coverage validator** (AC: 2, 3, 4)
  - [x] Add a standard-library validator (for example `tools/validate-coverage.py`) that recursively discovers Cobertura attachments, validates XML, canonicalizes separators/source roots, applies the checked-in scope, and unions source-line hits.
  - [x] Content-deduplicate or safely union the byte-identical UUID and `_HOST.../In/...` attachment copies currently produced per project. Never double-count a line or trust the sum of report-level `lines-valid` values.
  - [x] Require every configured production assembly/scope and explicit executable line evidence for `src/Hexalith.Memories.Server/Program.cs`, `src/Hexalith.Memories.Cli/Program.cs`, and `src/Hexalith.Memories.Mcp/Program.cs`.
  - [x] Print a concise console/GitHub summary and return nonzero for all fail-closed conditions and for `< 78.0%` (78.0 exactly passes). Do not silently skip an unreadable report.
  - [x] Add deterministic Python fixture tests under `tests/tooling/coverage_gate/` for every AC4 pass/fail/merge/path case. Follow the repository's existing `tools/verify-integration-stub-closure.py` plus `tests/tooling/integration_stub_closure/` pattern.

- [x] **Task 4 — Wire collection, validation, and evidence into CI** (AC: 1, 2, 4, 8)
  - [x] Update only the existing `.github/workflows/ci.yml` `test-unit-contract` job: call the existing test wrapper with `--coverage`, then call the validator with the checked-in configuration.
  - [x] Run the coverage-tooling fixture tests in the normal CI path before relying on the validator.
  - [x] Preserve the existing Release restore/build, kubectl-backed deployment contract tests, per-project inventory, TRX zero-test defense, and unrelated jobs.
  - [x] Keep artifact upload under `if: always()` and include all TRX/Cobertura outputs with `if-no-files-found: error` and 14-day retention.
  - [x] Measure the combined clean-run cost after enabling Coverlet and set an explicit `test-unit-contract` timeout with bounded margin. The current 25-minute value is not presumed sufficient, and timeout growth must be justified by observed collection/validation time rather than made unbounded.
  - [x] Do not collect coverage by invoking the whole `.slnx`; the root project inventories prevent filtered zero-test projects and are the canonical automation surface.

- [x] **Task 5 — Close the Epic 25 package/topology PR-lane gap** (AC: 7, 8, 9)
  - [x] Add bounded steps to the existing required `test-unit-contract` job that run both `python3 -m unittest discover -s tests/tooling/release_packages -p "*_test.py"` and `python3 -m unittest discover -s tests/tooling/publish_nuget -p "*_test.py"`; preserve the already-executed `publish_containers` fixtures and reuse that job's standard checkout, build-submodule initialization, pinned-SDK initialization, restore, and Release build. Include package-mode time in the measured timeout budget from Task 4.
  - [x] Pack the approved projects from `tools/release-packages.json` with one synthetic stable version into a clean `artifacts/packages/ci`-style directory, then validate the produced set with `tools/validate-release-packages.ps1 -PackageDirectory ... -Version ...`. Require exactly the nine approved packages and no extras.
  - [x] Add a narrowly named package-only switch to `tools/pack-release.ps1` and use that same inventory-driven build/pack/real-package-validation path in PR CI. The default release invocation must remain byte-for-behavior compatible and continue into container/deployment preparation; package-only mode must return successfully only after `-PackageDirectory` validation. Cover both branches in the existing release orchestration fixtures.
  - [x] Keep the PR exercise offline with respect to NuGet publication: no API key, `dotnet nuget push`, semantic-release, GitHub Release, or package upload to a registry. Local artifact upload is optional and must never substitute for validation.
  - [x] Update CI contract tests and `CONTRIBUTING.md` so the package/topology check is discoverable and its pack-plus-`-PackageDirectory` contract cannot silently return to post-merge-only execution.

- [x] **Task 6 — Add the nightly benchmark job without weakening it** (AC: 5, 6, 8)
  - [x] Extend `.github/workflows/nightly.yml`; do not create a second scheduled workflow. Keep `workflow_dispatch` and the daily cadence. The current `0 3 * * *` trigger is at the top of the hour; moving it to a nonzero minute is permitted to reduce peak scheduling delay, but is not required for A42 closure.
  - [x] Initialize the same build submodules/.NET SDK as current jobs, restore/build Release, verify Docker, and run `bash ./tools/test.sh --filter "Category=Benchmark" --configuration Release --no-build --results-directory TestResults/benchmark`.
  - [x] Update `tools/test.sh` and `tools/test.ps1` symmetrically so the **exact** `Category=Benchmark` selector chooses `tools/test-projects.benchmark.txt` but passes no trait filter to `dotnet test`; preserve normal filtering for every other expression. Add drift/runner-contract tests proving all 17 benchmark-project tests are selected rather than the current 13.
  - [x] Give the job a bounded timeout at least as large as the fixture/test five-minute bounds plus image pull/build margin. Reuse the benchmark project's pinned Testcontainers images; DAPR initialization is unnecessary for this suite.
  - [x] Upload the benchmark TRX and the exact Release `benchmark-results.json` in separate fail-closed artifact steps under `if: always()` so one file cannot mask the other's absence.
  - [x] Keep `ThesisValidation_HybridOutperforms80Percent`, `ReproducibilityTest_SameDatasetProducesIdenticalScores`, the full Category=Benchmark inventory, and the failing exit status. No selective `--filter`, retry-to-green, `continue-on-error`, or JSON post-processing.

- [x] **Task 7 — Add drift guards and update documentation** (AC: 3, 4, 5, 6, 7)
  - [x] Source/fixture-test `.github/workflows/ci.yml`, `.github/workflows/nightly.yml`, `tests/tests.runsettings`, the threshold configuration, and test inventories for the required commands/paths without using brittle bare-substring checks where parsed YAML/XML/JSON or table-row anchors are practical.
  - [x] Update `tests/README.md` with the local coverage collection + validation sequence, the scheduled/manual benchmark command, artifact locations, Docker requirement, 78.0% scoped line gate, and the current 80% thesis requirement.
  - [x] Update `CONTRIBUTING.md` with the current six-project Docker-free inventory, coverage/package PR gates, whole-project benchmark-selector behavior, and stable CI check/artifact guidance. Remove the stale `tests/README.md` sentence claiming the repository has no root CI workflow; keep the existing fast/slow integration taxonomy and per-project runner guidance.

- [x] **Task 8 — Run and record the gates** (AC: 9)
  - [x] Run the coverage-tooling unit tests and any workflow/runsettings contract tests.
  - [x] Build `Hexalith.Memories.slnx` in Release with the pinned SDK and zero warnings/errors.
  - [x] Run the six-project Docker-free lane with coverage into a clean results directory; require 4,374 passed/1 expected skip or reconcile any legitimate count change, then run the validator and retain its summary.
  - [x] Confirm the merged report contains executable lines for all three required `Program.cs` files and remains at or above 78.0%.
  - [x] Run the release-package and publish-NuGet fixtures; pack the synthetic version; require nine validated packages from the real `-PackageDirectory` path; and confirm no publish command or credential is used.
  - [x] With Docker, run the Category=Benchmark project through the wrapper and retain TRX/JSON. Require 17 executed tests; record the expected 16 passed/1 failed and current 75% thesis result as a known pre-existing red unless separately owned work has already corrected it. Do not absorb that correction into this diff.
  - [x] Run `git diff --check`; confirm no product source, benchmark corpus/algorithm, package version, submodule pointer, generated build artifact, or unrelated story tracking file entered the diff.

### Review Findings

- [x] [Review][Patch] Reject out-of-repository Cobertura source roots [tools/validate-coverage.py:134]
- [x] [Review][Patch] Validate coverage configuration types and finite thresholds [tools/validate-coverage.py:181]
- [x] [Review][Patch] Require complete nonempty coverage evidence for every inventory project [tools/validate-coverage.py:386]
- [x] [Review][Patch] Bind assembly evidence to the assembly's declared source scope [tools/validate-coverage.py:316]
- [x] [Review][Patch] Enforce exactly 17 executed benchmark tests from TRX evidence [tools/test.sh:155]
- [x] [Review][Patch] Prevent stale coverage attachments from contaminating repeated runs [tools/test.sh:107]
- [x] [Review][Patch] Emit per-assembly diagnostics when coverage is below threshold [tools/validate-coverage.py:495]
- [x] [Review][Patch] Structurally verify that workflow gates remain executable and blocking [tests/tooling/coverage_gate/coverage_contract_test.py:93]
- [x] [Review][Patch] Resolve filenames across multiple valid Cobertura source roots [tools/validate-coverage.py:134]
- [x] [Review][Patch] Test the coverage validator's CLI exit-code contract [tests/tooling/coverage_gate/validate_coverage_test.py:356]
- [x] [Review][Patch] Test package-only validation failure propagation [tests/tooling/publish_containers/release_orchestration_test.py:326]
- [x] [Review][Patch] Verify mapped absolute POSIX and Windows coverage paths [tests/tooling/coverage_gate/validate_coverage_test.py:335]
- [x] [Review][Patch] Pin the benchmark thesis and reproducibility assertions, not only their names [tests/tooling/coverage_gate/test_runner_contract_test.py:45]
- [x] [Review][Patch] Reconcile the stale story completion status [26-4-coverage-gating-and-benchmark-lane.md:306]

## File Scope

Allowed files for this story:

- `.github/workflows/ci.yml` - UPDATE. Add blocking coverage validation and the pre-merge package/topology lane while preserving every existing job.
- `.github/workflows/nightly.yml` - UPDATE. Add the bounded whole-project benchmark job under the existing triggers.
- `tests/tests.runsettings` - UPDATE. Repair XML, remove the blanket composition-root exclusion, and narrow first-party coverage collection.
- `tests/README.md` - UPDATE. Document local/CI coverage and benchmark commands, artifacts, and thresholds.
- `CONTRIBUTING.md` - UPDATE. Reconcile the six-project inventory and document the coverage, package, and benchmark automation.
- `tools/validate-coverage.py` - NEW. Fail-closed scoped Cobertura union and threshold validator.
- `tools/coverage-thresholds.json` - NEW. Single source for threshold, production scope, required assemblies, and composition roots.
- `tests/tooling/coverage_gate/**` - NEW. Synthetic validator fixtures plus workflow/runsettings/config/runner contracts.
- `tools/test.sh` - UPDATE. Preserve existing behavior and make exact `Category=Benchmark` an inventory-only selector.
- `tools/test.ps1` - UPDATE. Keep parity with the Bash runner for exact benchmark inventory selection.
- `tools/pack-release.ps1` - UPDATE only for the tested package-only mode reused by PR CI; default release behavior must remain unchanged.
- `tests/tooling/publish_containers/**` - UPDATE only for release-orchestration fixtures that prove package-only mode stops before container/deployment work.
- `tests/tooling/release_packages/**` - UPDATE only for a focused PR-workflow or package-only-mode contract that cannot live in the coverage tooling suite.
- `tests/tooling/publish_nuget/**` - UPDATE only for a focused PR-workflow contract that cannot live in the coverage tooling suite.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` - UPDATE only for the existing strongly anchored workflow/inventory/release seam guards.
- `_bmad-output/implementation-artifacts/26-4-coverage-gating-and-benchmark-lane.md` - UPDATE. Record implementation evidence, completion notes, and the final file list.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad workflow/status transitions.

Read/verify only:

- `tools/release-packages.json`
- `tools/validate-release-packages.ps1`
- `tools/test-projects.unit-contract.txt`
- `tools/test-projects.benchmark.txt`
- `tests/Hexalith.Memories.Benchmarks/**`
- `.github/workflows/release.yml`
- `Directory.Packages.props`
- `global.json`

Forbidden by default:

- `src/**`
- Benchmark assertions, traits, corpus, ground truth, algorithms, or generated results
- Package inventory, version pins, schemas, or publish behavior
- `.github/workflows/release.yml`
- `tools/publish-nuget.ps1`
- `tools/validate-release-packages.ps1`
- `tools/test-release.ps1`
- `references/**` contents or submodule pointers
- Generated `TestResults/**`, `bin/**`, `obj/**`, coverage XML, `.nupkg`, and `benchmark-results.json` artifacts

## Dev Notes

### Non-negotiable implementation contract

- This story creates trustworthy automation; it does not improve a number by excluding hard code, broadening attributes, skipping tests, or changing product behavior.
- The gate denominator is the union of first-party production sequence points, not a sum or average of six overlapping Cobertura roots. Dependencies appear in more than one report, and each project currently produces two byte-identical attachment paths.
- Keep test assemblies and external/submodule sources out of the production denominator explicitly. The diagnostic all-assembly union is only 70.95% because it includes FrontComposer, TestHelpers, Web.Specimens, and `MigrateEmbeddingVectors`; that is not the intended A42 metric.
- A missing assembly/report is not 100% and not “not applicable.” Fail unless the checked-in scope is deliberately changed with tests and rationale.
- Removing the blanket `Program.cs` exclusion is mandatory even though Story 25.1 reduced Server `Program.cs` from the audit-era hotspot to a thin composition root. The anti-pattern remains dangerous for future roots.
- Keep coverage collection on VSTest for this story. `tests.runsettings` is a VSTest facility; native Microsoft.Testing.Platform uses different coverage extensions/options. Mixing `coverlet.collector` and `coverlet.MTP` would create two semantics in one lane.
- The benchmark project is an xUnit/Testcontainers quality suite, not BenchmarkDotNet microbenchmarks. Do not add BenchmarkDotNet or move it into the `Category=Performance` lane.
- The exact `Category=Benchmark` wrapper value is an inventory selector, not a trait subset: after choosing the one-project inventory it must run all 17 tests. Compound or other filters retain normal `dotnet test --filter` semantics.
- The package PR gate is a dry validation lane, not a release. It must exercise real packed nuspecs and the publish script's fixtures without using credentials or contacting NuGet for publication.
- Story 26.4 owns truthful automation, not retrieval tuning. A red scheduled result can close this story when the complete lane and artifacts work as specified; the open Epic 26 readiness action—not a weakened CI result—holds the epic until the 80% product gate is restored or formally changed.
- GitHub scheduled workflows run from the default branch. Use `workflow_dispatch` for implementation verification before waiting for the next schedule; do not claim the schedule itself executed until a real GitHub run exists.

### Measured coverage baseline (2026-07-13)

The checked-in runsettings could not run because of invalid XML. A temporary diagnostic copy only fixed the comment and removed the `Program.cs` exclusion; no repository source was changed. The six Release test projects passed 4,374 tests with one existing skip and yielded this source-line union:

| First-party project | Covered / valid | Line coverage |
|---|---:|---:|
| `Hexalith.Memories.Server` | 16,441 / 21,760 | 75.56% |
| `Hexalith.Memories.Cli` | 3,330 / 4,145 | 80.34% |
| `Hexalith.Memories.Client.Rest` | 463 / 539 | 85.90% |
| `Hexalith.Memories.Contracts` | 1,286 / 1,309 | 98.24% |
| `Hexalith.Memories.EventStore` | 452 / 1,119 | 40.39% |
| `Hexalith.Memories.Mcp` | 719 / 787 | 91.36% |
| `Hexalith.Memories.ServiceDefaults` | 311 / 361 | 86.15% |
| `Hexalith.Memories.Telemetry` | 96 / 103 | 93.20% |
| `Hexalith.Memories.Web` | 2,892 / 2,991 | 96.69% |
| **Union** | **25,990 / 33,114** | **78.49%** |

Composition-root evidence in that diagnostic run: Server `Program.cs` 52/56, CLI `Program.cs` 41/49, MCP `Program.cs` 10/10. These are guard targets, not separate percentage thresholds.

### Known benchmark quality debt

- `BenchmarkSuiteTests` has three tests under both `Category=Benchmark` and `Category=Integration`: result validity/artifact emission, the hard 80% thesis assertion, and two-run reproducibility.
- The project also has ten `NdcgScorerTests` carrying `Category=Benchmark` and four untraited `BenchmarkSeederTests`. The wrappers currently forward the exact trait filter and therefore select 13/17; Story 26.4 must make the inventory selector execute all 17. The current whole-project baseline is 16 passed/1 failed, with the single failure at the thesis assertion.
- The Release JSON dated 2026-07-11 contains eight queries, six hybrid wins, a 0.75 win rate, and `thesisValidated=false`; BQ-03 and BQ-07 are the losses.
- The PRD says 80% is a hard line, not a stretch goal, and NFR26 requires identical NDCG@10 for two same-dataset runs. A permanently ignored/advisory job would not catch regression and does not close the intent.
- Do not game this result. If the new nightly job is red, its automation is still truthful and this CI story can complete; the linked Epic 26 readiness action remains open until separate retrieval-quality ownership decides/fixes the product result. Neither the story nor the epic may claim a green benchmark result while it is 75%.

### Files to update or create

| Path | Action | Current state and required preservation/change |
|---|---|---|
| `.github/workflows/ci.yml` | UPDATE | Preserve all existing jobs/check names, Release build, inventories, zero-test/TRX defense, integration-fast verifier, deployment verification, and 14-day artifacts. Add coverage, validator, and bounded real-package/topology steps to `test-unit-contract`. |
| `.github/workflows/nightly.yml` | UPDATE | Preserve fast/slow integration jobs and current triggers. Add a separate bounded benchmark job using the existing wrapper/inventory. |
| `tests/tests.runsettings` | UPDATE | Currently invalid XML and excludes all `Program.cs`. Repair XML, remove blanket exclusion, and scope Coverlet to first-party production assemblies. |
| `tests/README.md`, `CONTRIBUTING.md` | UPDATE | Document the real root workflows, coverage/package gates, whole-project nightly/manual benchmark, artifacts, thresholds, and known requirements. Preserve existing lane taxonomy. |
| `tools/validate-coverage.py` | NEW | Fail-closed Cobertura discovery, source-line union, scope/required-root enforcement, threshold, and summary. Standard library only. |
| `tools/coverage-thresholds.json` | NEW | Single source for 78.0%, first-party scope, required assemblies, and required composition-root paths. Name may vary, but do not duplicate configuration in CI/tests. |
| `tests/tooling/coverage_gate/*_test.py` | NEW | Validator fixtures plus workflow/runsettings/config drift contracts. Follow existing Python unittest layout. |
| `tools/test.sh`, `tools/test.ps1` | UPDATE | Preserve coverage, inventories, Release/`--no-build`, TRX, and zero-test checks. Make exact `Category=Benchmark` select the benchmark inventory while clearing the effective trait filter in both wrappers. |
| `tools/pack-release.ps1` | UPDATE | Add a tested package-only switch that reuses existing inventory-driven build/pack/`-PackageDirectory` validation and stops before container/deployment preparation; preserve default release behavior. |
| `tools/release-packages.json`, `tools/validate-release-packages.ps1` | PRESERVE | Existing nine-package inventory and fail-closed package validator are the gate sources. Do not duplicate or relax them. |
| `tests/tooling/release_packages/**`, `tests/tooling/publish_nuget/**`, `tests/tooling/publish_containers/**` | PRESERVE / UPDATE TESTS ONLY IF NEEDED | Run the first two in PR CI, preserve the third, and add only contract coverage needed for a shared package-only seam or workflow drift guard. |
| `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` | UPDATE IF NEEDED | Preferred existing home for strongly anchored inventory/workflow/release-script contract checks that benefit from its current parsers. |
| `tools/test-projects.unit-contract.txt`, `tools/test-projects.benchmark.txt` | PRESERVE | These are the canonical inventories. Change only if live project reconciliation proves them stale, with guard tests. |
| `tests/Hexalith.Memories.Benchmarks/**` | PRESERVE | No algorithm, assertion, corpus, ground-truth, fixture, trait, or result-shape edits in this CI-only story. |

### Testing requirements

- Python tooling tests use `unittest`, temp directories, and synthetic minimal Cobertura XML; do not depend on existing `TestResults/` or the local machine name.
- A passing fixture must include overlapping reports to prove union behavior. A duplicate attachment must not change totals.
- Threshold tests cover exactly-at-threshold pass and one-line/percentage-below failure.
- Windows paths (`\`) and POSIX paths (`/`) normalize to the same repository-relative identity.
- The validator must reject path traversal/out-of-repo source identities rather than accidentally including them.
- Workflow contract tests should parse XML/JSON and use structured YAML checks where available. If a source anchor is unavoidable, tie it to the complete command/job structure so an unrelated comment cannot satisfy it.
- Runner contract tests must distinguish inventory selection from trait filtering: exact `Category=Benchmark` yields the benchmark project with no effective filter, while compound/other filters remain unchanged.
- Package-lane tests must prove the PR workflow runs both fixture suites, produces/validates the real nine-package set with a synthetic version, supplies both `-PackageDirectory` and `-Version`, and never invokes a publication command.
- CI artifacts are evidence, not a substitute for job failure. Upload under `if: always()` while preserving the original test/validator exit status.

### Previous story intelligence

- Story 26.3 established the repo pattern for an executable Python verifier, deterministic fixture tests under `tests/tooling/`, a checked-in inventory, TRX outcome validation, and a separate CI verification step. Reuse that shape; do not merge coverage semantics into `verify-integration-fast-coverage.py` or `verify-integration-stub-closure.py`.
- The full integration lane now contains eight intentional structured skips. Coverage gating must not relabel them or pretend the Docker-free lane executes them.
- Story 26.3's most important lesson is evidence honesty: source presence is not execution proof, zero tests are failure, and a green placeholder is worse than an explicit red/skip. Apply the same rules to reports and benchmark artifacts.
- Epic 25.8's retrospective explicitly hands Story 26.4 the post-merge-only package/topology gap. Preserve its inventory/validator behavior and bring the real pack validation plus `release_packages`/`publish_nuget` fixtures into PR CI.
- Preserve all 26.3 fixture, DAPR, topology, submodule, and sprint tracking changes; this story should not touch them.

### Git intelligence

- Baseline is `1ce41926feca03bdd4bbe74e053db45535980421`. Recent work completed the 26.2/26.3 recovery and failure-mode test slices; no coverage/nightly implementation is present in those commits.
- Commit `e773630` added the reusable verifier pattern (`tools/verify-integration-stub-closure.py`, Python fixture tests, manifest, CI-fast evidence) and is the most relevant implementation precedent.
- Recent commits include broad line-ending/file churn and submodule pointer changes. Story 26.4 must keep its file list narrow and leave all root-declared submodule pointers unchanged.
- Use a non-release Conventional Commit such as `ci: add coverage and benchmark gates`; do not label this CI/tooling story `feat`.

### Latest technical information (verified 2026-07-13)

- Coverlet's VSTest collector is invoked with `dotnet test --collect:"XPlat Code Coverage"` and writes `coverage.cobertura.xml` under TestResults. The pinned `Microsoft.NET.Test.Sdk` 18.7.0 exceeds Coverlet's documented 17.12+ collector requirement.
- `tests.runsettings` only applies to VSTest. Native Microsoft.Testing.Platform uses `coverlet.MTP` and `--coverlet`; do not mix that migration into this story while `TestingPlatformDotnetTestSupport=False`.
- GitHub schedules execute the latest default-branch commit and can be delayed at the start of the hour. The existing `0 3 * * *` schedule is already a valid home; manual dispatch is the immediate verification path.
- GitHub workflow artifacts are the supported way to retain coverage/test output after jobs. Set `if-no-files-found: error` for evidence whose absence invalidates the gate.

### Project structure notes

- No product C#, persistence, API, DAPR, Aspire topology, UX, FrontComposer, localization, or database changes are required. If implementation appears to require one, stop and split the defect into the owning story.
- Do not modify files under `references/`; persistent submodule facts are guidance only. Root Memories rules and versions govern this story.
- UI/UX artifacts only establish that benchmark evidence may later feed the already-owned Benchmark Result Comparator. No UI work belongs here.
- Generated `TestResults/`, `bin/`, `obj/`, and `benchmark-results.json` files are validation artifacts and must not be committed.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md`, Epic 26 / Story 26.4] — canonical statement, A42, coverage threshold, and nightly benchmark intent (`:4547-4599`).
- [Source: `_bmad-output/implementation-artifacts/epic-26-context.md`] — no composition-root exclusion, reproducible scheduled NDCG suite, Docker-free unit/contract boundary, and 26.3→26.4 dependency (`:17-41`).
- [Source: `_bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md`, A42] — missing CI collection/threshold, `Program.cs` exclusion, and missing benchmark lane (`:73-90`).
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md`, Epic 26] — Test Architect ownership and A42 closure (`:144-164`).
- [Source: `_bmad-output/planning-artifacts/prd.md`] — 80% hard kill switch, NDCG@10 protocol, FR25, deterministic fusion, and NFR26 reproducibility (`:28`, `:62`, `:93-104`, `:875`, `:1002-1008`).
- [Source: `_bmad-output/planning-artifacts/architecture.md`, Test Patterns] — xUnit v3, Shouldly, NSubstitute, Coverlet, tiers, and naming (`:1139-1165`).
- [Source: `_bmad-output/implementation-artifacts/26-3-integration-stub-closure.md`] — predecessor evidence, verifier pattern, lane results, and accepted skips.
- [Source: `_bmad-output/implementation-artifacts/epic-25-retro-2026-07-12.md`] — Story 26.4 package/topology carry-forward, missing PR discovery for `publish_nuget`, and 16/17 benchmark baseline (`:55`, `:80`, `:101-113`).
- [Source: `_bmad-output/implementation-artifacts/spec-25-8-dead-code-and-topology-cleanup.md`] — deferred real-package PR validation and current release fixture evidence (`:129`, `:161`).
- [Source: `.github/workflows/ci.yml`] — current Docker-free job, project runner, artifacts, and absence of coverage invocation.
- [Source: `.github/workflows/nightly.yml`] — current schedule/manual trigger and fast/slow integration-only jobs.
- [Source: `tests/tests.runsettings`] — current Cobertura collector, invalid XML comment, and blanket `Program.cs` exclusion.
- [Source: `tools/test.sh`, `tools/test.ps1`, `tools/test-projects.unit-contract.txt`, `tools/test-projects.benchmark.txt`] — existing coverage switches, inventories, TRX, and zero-test guard.
- [Source: `tools/pack-release.ps1`, `tools/release-packages.json`, `tools/validate-release-packages.ps1`] — existing inventory-driven package build and real `-PackageDirectory` validation seam.
- [Source: `tests/tooling/release_packages/`, `tests/tooling/publish_nuget/`] — package/topology and publication fixture suites currently absent from PR CI.
- [Source: `tests/Hexalith.Memories.Benchmarks/BenchmarkSuiteTests.cs`] — 80% thesis, reproducibility, result artifact, and benchmark traits.
- [Source: `tests/Hexalith.Memories.Benchmarks/Fixtures/BenchmarkFixture.cs`] — pinned Testcontainers topology and five-minute setup bound.
- [Source: `tests/Hexalith.Memories.Benchmarks/bin/Release/net10.0/benchmark-results.json`] — local diagnostic 6/8 (75%) result observed during story creation; the file is gitignored, non-authoritative, and must be regenerated in fresh workspaces.
- [Microsoft .NET unit-test coverage](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage) — official `XPlat Code Coverage`/Cobertura guidance.
- [Microsoft Testing Platform coverage](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-code-coverage) — native MTP coverage and `coverlet.MTP` are a different integration.
- [Coverlet](https://github.com/coverlet-coverage/coverlet) — official collector requirements and VSTest usage.
- [xUnit v3 runsettings](https://xunit.net/docs/config-runsettings) — runsettings are VSTest-only.
- [GitHub scheduled workflow events](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#schedule) — default-branch/scheduling behavior.
- [GitHub workflow artifacts](https://docs.github.com/en/actions/concepts/workflows-and-actions/workflow-artifacts) — retaining test and coverage evidence.
- [upload-artifact](https://github.com/actions/upload-artifact#if-no-files-are-found) — fail behavior for missing artifact paths.

## Project Context Reference

Follow `_bmad-output/project-context.md` and `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`. In particular: use `.slnx`; keep package versions centralized; run test projects through the repository inventories; preserve warnings-as-errors; do not commit generated artifacts; initialize only root-declared submodules; and do not modify submodule contents/pointers for this story. The persistent EventStore, FrontComposer, and Tenants project contexts are dependency guidance only and do not override this repository's CI/test conventions.

## Story Completion Status

- Status set to `done`.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- A42 and the assigned Epic 25 carry-forward are fully contexted with a measured 78.49% baseline, a 78.0% fail-closed ratchet, composition-root guards, real package/topology PR validation, whole-project benchmark execution, and a separately tracked Epic 26 readiness action for the current 75% thesis failure.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-13 Task 1 preflight: implementation-start HEAD `7593f4bbf0a1462e77cda67a4839f4d0f5d48d4f`; the story's preserved baseline is `1ce41926feca03bdd4bbe74e053db45535980421`. The moved HEAD commit contains story/tracking creation and a pre-existing FrontComposer pointer update, not Story 26.4 implementation; this implementation leaves `references/**` untouched.
- 2026-07-13 Task 1 platform baseline: .NET SDK 10.0.301; VSTest collector path; coverlet.collector 10.0.1; Microsoft.NET.Test.Sdk 18.7.0; xUnit v3 3.2.2; xunit.runner.visualstudio 3.1.5. Twelve Cobertura attachments (six byte-identical path pairs) exist under `TestResults/story-26-4-coverage-baseline`.
- 2026-07-13 Task 1 evidence baseline: Docker-free inventory 4,374 passed/1 skipped/0 failed; diagnostic source-line union 25,990/33,114 (78.49%) with Server/CLI/MCP `Program.cs` evidence restored. Local ignored benchmark JSON remains 6/8 wins (75%), `thesisValidated=false`.
- Implementation plan: establish the checked-in scope contract first, then repair collection, implement and fixture-test the fail-closed union validator, wire CI/package/nightly lanes, add drift guards/docs, and execute the complete gates without changing product or benchmark behavior.
- 2026-07-13 Task 2 red/green: XML parsing initially failed at the double-hyphen coverage comment. After repair and scope filters, two coverage contract tests pass and a real six-project Coverlet run produced six Cobertura reports with 4,374 passed/1 skipped/0 failed.
- 2026-07-13 Task 3 red/green/refactor: validator fixtures first failed because `tools/validate-coverage.py` was absent. The first real-report validation then exposed `repo/src` source roots with project-relative filenames; a new regression fixture pinned that shape before normalization was corrected. Final evidence: 13 tooling tests pass; six unique reports plus six duplicates union to 25,990/33,114 (78.49%); Server/CLI/MCP roots expose 56/49/10 executable lines; full Docker-free regression remains 4,374 passed/1 skipped/0 failed.
- 2026-07-13 Task 4 red/green: the job-scoped contract initially failed on the old 25-minute timeout. CI now runs 14 coverage fixtures, wrapper-based collection, validator execution, and fail-closed 14-day artifact retention in the existing required job. Measured local post-build collection was 36.90 seconds and validation 0.23 seconds; the lane produced 4,374 passed/1 skipped/0 failed and 25,990/33,114 (78.49%). Timeout is bounded at 40 minutes to include shared Release restore/build, kubectl contracts, coverage, and the Task 5 real-package gate.
- 2026-07-13 Task 5 red/green: package-only and CI contracts first failed because the switch/steps were absent. The first real pack then correctly rejected prerelease `0.0.0-ci.264`; the test and workflow were tightened to stable `0.0.264`. Final evidence: release_packages 28/28 (56.46s), publish_nuget 8/8 (23.01s), publish_containers/release orchestration 10/10 (28.06s), package-only build/pack/validation 30.50s with 0 warnings/errors and exactly nine `.nupkg` files, plus Docker-free regression 4,374 passed/1 skipped/0 failed. These measured additions remain comfortably bounded by the 40-minute job timeout.
- 2026-07-13 Task 6 red/green/evidence: job and wrapper contracts initially failed because the nightly job was absent and both wrappers forwarded the exact selector. Final 19 coverage/runner contracts pass. Docker 29.4.3 executed the whole project with no trait filter: 17 total, 16 passed, 1 failed at `ThesisValidation_HybridOutperforms80Percent`; JSON records 6/8 wins, 75%, `thesisValidated=false`, while reproducibility and all four untraited seeder tests passed. The failure remains blocking and is not described as green. Docker-free regression remains 4,374 passed/1 skipped/0 failed.
- 2026-07-13 Task 7 red/green: the documentation contract first failed on stale coverage/CI guidance. Final 20 tooling contracts parse JSON/XML, scope assertions to specific workflow jobs, execute both wrappers through a fake test host, pin the benchmark inventory/count/assertion names, and guard contributor commands, thresholds, artifacts, and six-project inventory. Full Docker-free regression remains 4,374 passed/1 skipped/0 failed.
- 2026-07-13 Task 8 gate evidence: 21 coverage/workflow/runsettings/runner/documentation contracts pass; the Release solution build completed with 0 warnings/errors; the clean six-project coverage lane completed 4,374 passed/1 skipped/0 failed and unioned six unique plus six duplicate attachments to 25,990/33,114 (78.49%), including 56/49/10 executable Server/CLI/MCP `Program.cs` lines. Release-package, publish-NuGet, and release-orchestration fixtures completed 28/8/10 passes; package-only mode validated exactly nine stable `0.0.264` packages through the real `-PackageDirectory` path without publication. Docker executed all 17 benchmark tests and retained TRX/JSON with the known pre-existing result of 16 passed/1 thesis failure, 6/8 wins (75%), `thesisValidated=false`. `git diff --check` is clean, generated evidence remains ignored, and the version-controlled gate configuration was moved into the allowed `tests/tooling/coverage_gate/**` scope after an ignore guard exposed the repository's `coverage*.json` rule.
- 2026-07-13 final review regression: the post-implementation six-project Docker-free inventory again completed 4,374 passed/1 expected skip/0 failed with nonzero execution verified from every TRX.
- 2026-07-15 adversarial review remediation: closed all 14 patch findings. Coverage evidence now rejects external/ambiguous roots, malformed configuration, assembly/source mismatches, stale attachments, and missing/empty project reports; below-threshold runs retain per-assembly console/GitHub diagnostics. Both wrappers clean repository-local result roots and require benchmark TRX evidence of exactly 17 executed/0 skipped tests before propagating the known thesis failure. Evidence: coverage/workflow/runner fixtures 30/30, release orchestration fixtures 5/5, Docker-free inventory 4,428 passed/1 expected skip/0 failed, hardened union 26,721/34,191 (78.15%) with six reports and all composition roots, Release solution build 0 warnings/errors, and `git diff --check` clean.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Task 1 complete: added the single-source 78.0% coverage contract with nine required assemblies and three required composition roots; its focused unittest and the full six-project 4,374 passed/1 skipped regression inventory pass.
- Task 2 complete: repaired `tests.runsettings`, restored composition-root collection, narrowed collector scope to first-party production assemblies, preserved the pinned VSTest/Coverlet seam, and verified collection across all six Docker-free test projects.
- Task 3 complete: added a standard-library, fail-closed Cobertura validator with content deduplication, safe source-root/path normalization, source-line union, per-assembly diagnostics, required assembly/root evidence, exact threshold enforcement, console output, and GitHub step summaries.
- Task 4 complete: made scoped coverage a blocking path in the existing PR/push job, with gate fixtures before collection, validator enforcement after all six inventory projects, fail-closed evidence retention, and a measured bounded timeout.
- Task 5 complete: added tested `-PackageOnly` orchestration, ran release/package/publication fixtures before merge, packed and validated the real nine-package set with stable synthetic version `0.0.264`, preserved default container/deployment preparation, and kept PR CI publication-free.
- Task 6 complete: added the bounded nightly NDCG job, inventory-only exact selector behavior in both wrappers, separate fail-closed TRX/JSON artifacts, and executable 17-test drift proof while preserving the known 75% quality failure.
- Task 7 complete: added durable workflow/runsettings/config/inventory/runner/documentation guards and reconciled local/CI guidance for coverage, package-only validation, benchmark execution, thresholds, Docker, and retained evidence.
- Task 8 complete: executed and recorded every required gate, retained truthful coverage/package/benchmark evidence, confirmed the configured threshold and composition roots, and verified a scope-clean diff with no product, benchmark, version, submodule, generated-artifact, or publication changes.
- Code review remediation complete: applied and verified all 14 adversarial findings; no decisions or deferred items remain, and four review claims were dismissed after call-path and Git-history verification.

### File List

- `.github/workflows/ci.yml` (modified)
- `.github/workflows/nightly.yml` (modified)
- `CONTRIBUTING.md` (modified)
- `_bmad-output/implementation-artifacts/26-4-coverage-gating-and-benchmark-lane.md` (modified)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified)
- `tests/tests.runsettings` (modified)
- `tests/README.md` (modified)
- `tests/tooling/coverage_gate/coverage_contract_test.py` (new)
- `tests/tooling/coverage_gate/test_runner_contract_test.py` (new)
- `tests/tooling/coverage_gate/validate_coverage_test.py` (new)
- `tests/tooling/publish_containers/release_orchestration_test.py` (modified)
- `tests/tooling/coverage_gate/line-coverage-gate.json` (new)
- `tools/pack-release.ps1` (modified)
- `tools/test.ps1` (modified)
- `tools/test.sh` (modified)
- `tools/validate-coverage.py` (new)

## Change Log

- 2026-07-13: Implemented blocking scoped coverage and package validation in PR CI, a fail-closed union coverage validator and drift guards, whole-project nightly benchmark execution with durable evidence, symmetric test-runner behavior, and updated contributor/test documentation. Verified the known benchmark quality result remains truthfully red at 75%.
- 2026-07-15: Applied all adversarial review patches, hardening coverage trust boundaries and completeness, benchmark execution-count evidence, blocking workflow contracts, CLI/package failure propagation, repeated-run cleanup, and story status consistency.
