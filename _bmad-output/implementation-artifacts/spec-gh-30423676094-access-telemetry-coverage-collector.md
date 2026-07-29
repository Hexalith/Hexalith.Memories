---
title: 'Restore AccessTelemetry coverage collection in CI'
type: 'bugfix'
created: '2026-07-29'
status: 'done'
review_loop_iteration: 1
baseline_commit: '0c351ff970b39a80a90821020feb0e6e8faf0183'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/26-4-coverage-gating-and-benchmark-lane.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Main CI run `30423676094` executes all 88 AccessTelemetry tests but
fails the required coverage gate because
`Hexalith.Memories.AccessTelemetry.Tests` cannot resolve the `XPlat Code
Coverage` data collector and therefore emits no Cobertura report. The coverage
configuration correctly requires that project's evidence, so removing it from
the gate would hide the defect.

**Approach:** Add the repository-pinned `coverlet.collector` package to the
AccessTelemetry test project using the same private asset shape as its sibling
test projects, extend the coverage contract fixtures so every configured
required-report project must retain that collector, and register the emitted
AccessTelemetry Clock and Contracts assemblies in the existing source-scope
map. Exercise every supported canonical operation tuple through the existing
AccessTelemetry contracts checkpoint tests so the honestly expanded scope
continues to satisfy the approved coverage ratchet, and exercise the real
Clock host, development UTC sources, and attestation endpoint through the
repository-pinned in-memory ASP.NET test host. Register the resulting main
AccessTelemetry assembly evidence and set the approved honest ratchet to
76.5% for the expanded production inventory.

## Boundaries & Constraints

**Always:** Keep package versions centralized; preserve the VSTest `XPlat Code
Coverage` and Cobertura seam; derive guarded project names from
`requiredReportProjects`; keep the report-project inventory and missing-report
failure unchanged; set the approved 76.5% threshold; add only
`Hexalith.Memories.AccessTelemetry`,
`Hexalith.Memories.AccessTelemetry.Clock` and
`Hexalith.Memories.AccessTelemetry.Contracts` to the required assembly/source
scope; use XML-aware fixture assertions.

**Coverage proof:** Add one behavior-focused theory for every supported
canonical operation tuple in the existing contracts checkpoint test file; do
not add assertions whose only purpose is touching property accessors or lines.
Add one in-memory Clock-host test that proves startup and a successful
development-source attestation through the HTTP endpoint.

**Ask First:** Changing the approved 76.5% coverage threshold or required
project inventory; changing the assembly inventory beyond the three approved AccessTelemetry
assemblies; changing the test runner or coverage technology; adding a package
version or changing the shared package catalog; modifying any project beyond
the AccessTelemetry test project.

**Never:** Remove AccessTelemetry from required coverage evidence; tolerate a
missing collector/report; switch to `coverlet.MTP`, `coverlet.msbuild`, or a
second collector; add an inline package version; rewrite completed Story 26.4
as if this later project-addition drift existed at its completion baseline.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| AccessTelemetry coverage | Required project has the private collector reference | Its complete test project runs and a Cobertura report is emitted | Any test or collection failure remains nonzero |
| Clock host | Development configuration with three local authenticated UTC sources | In-memory host starts and returns a signed attestation through the HTTP endpoint | Startup, dependency, signing, or endpoint failure remains nonzero |
| AccessTelemetry source scope | Emitted main, Clock, and Contracts production assemblies | All three assemblies are attributed only to their matching `src/` directories | Missing or mismatched mappings fail validation |
| Required-project drift | A configured required-report project loses or never adds the collector | Focused coverage contract fixture names the offending project | Fixture fails before the expensive coverage lane |
| Missing runtime report | Collection completes without a required project's Cobertura file | Existing union validator rejects the evidence set | Do not downgrade, exclude, or warn-only |

</frozen-after-approval>

## File Scope

Allowed files for this story:

- `tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj` -- add the versionless private collector and centrally pinned ASP.NET test-host references.
- `tests/tooling/coverage_gate/coverage_contract_test.py` -- guard collector ownership for every required-report project.
- `tests/tooling/coverage_gate/line-coverage-gate.json` -- register the three emitted AccessTelemetry production assemblies, their exact source prefixes, and the approved 76.5% ratchet.
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Contracts/AccessTelemetryContractsCheckpointTests.cs` -- verify canonicalization for every supported operation tuple and associated case/result rules.
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Clock/ClockAttestationCheckpointTests.cs` -- boot the real Clock host in memory and verify a development-source attestation response.
- `tests/README.md` -- publish the approved coverage threshold in local test guidance.
- `CONTRIBUTING.md` -- publish the same threshold in contributor workflow guidance.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- review-generated record for the pre-existing six-vs-seven documentation inventory drift.
- `_bmad-output/implementation-artifacts/spec-gh-30423676094-access-telemetry-coverage-collector.md` -- this specification and implementation evidence.

Read/verify only:

- `tests/tests.runsettings`
- `tools/test.sh`
- `tools/validate-coverage.py`
- `.github/workflows/ci.yml`
- `Directory.Packages.props`
- `references/Hexalith.Builds/Props/Directory.Packages.props`

Forbidden by default:

- production source or tests
- workflow, threshold, runner, or validator behavior
- package catalog and submodule pointers
- unrelated planning or implementation artifacts

## Historical Context Classification

| Source | Classification | Use |
|--------|----------------|-----|
| Story 26.4 | historical-reference-only | Retain its VSTest/Coverlet seam and fail-closed report contract; do not reuse its broad story shape or reopen it. |

## Slice Proof

One independently demonstrable outcome: the required AccessTelemetry test
project emits valid, source-attributed Cobertura evidence above the approved
76.5% ratchet, and a focused configuration-driven fixture prevents any
required-report project from silently shipping without the collector again.

## Code Map

- `tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj` -- the required-report project and its centrally versioned test dependencies.
- `tests/tooling/coverage_gate/coverage_contract_test.py` -- coverage configuration and workflow contract fixtures.
- `tests/tooling/coverage_gate/line-coverage-gate.json` -- authoritative report inventory, approved threshold, and assembly/source-scope map; add only the three AccessTelemetry mappings.
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Contracts/AccessTelemetryContractsCheckpointTests.cs` -- canonical operation contract coverage for the expanded production scope.
- `tests/Hexalith.Memories.AccessTelemetry.Tests/Clock/ClockAttestationCheckpointTests.cs` -- in-memory Clock-host and attestation endpoint proof.
- `tests/README.md` and `CONTRIBUTING.md` -- contributor-facing consumers of the configured threshold.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- review-owned deferral ledger, appended without changing prior entries.
- `tools/validate-coverage.py` -- existing fail-closed missing-report validator, unchanged.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj` -- add versionless `coverlet.collector` with `PrivateAssets=all` and the repository-standard `IncludeAssets` set.
- [x] `tests/tooling/coverage_gate/coverage_contract_test.py` -- parse each configured required project's project XML, require the correctly private collector reference, and derive contributor threshold assertions from configuration.
- [x] `tests/tooling/coverage_gate/line-coverage-gate.json` -- require all three emitted AccessTelemetry assemblies, bind each to its matching production source directory, and pin the approved 76.5% ratchet.
- [x] `tests/Hexalith.Memories.AccessTelemetry.Tests/Contracts/AccessTelemetryContractsCheckpointTests.cs` -- accept and round-trip every supported operation tuple through the canonicalizer.
- [x] Clock-host verification -- add the versionless `Microsoft.AspNetCore.Mvc.Testing` reference and prove a successful development-source attestation through the in-memory HTTP host.
- [x] Contributor documentation -- publish the configured 76.5% minimum consistently in `tests/README.md` and `CONTRIBUTING.md`.
- [x] Verification -- reproduce focused collection, run the coverage fixtures, then run the complete Docker-free coverage lane and validator.

**Acceptance Criteria:**
- Given AccessTelemetry remains in `requiredReportProjects`, when its complete Release test project runs with `XPlat Code Coverage`, then all tests pass and at least one `coverage.cobertura.xml` is emitted under that project's result directory.
- Given any configured required-report project lacks `coverlet.collector` or its private asset metadata, when coverage contract fixtures run, then the fixture fails and identifies that project.
- Given each supported access-telemetry operation and its valid tuple/query shape, when it is canonicalized and parsed, then the exact operation contract round-trips without weakening validation.
- Given the Clock host runs in development mode with its three local authenticated UTC sources and scoped signing material, when the attestation endpoint is invoked in memory, then it returns a valid signed attestation.
- Given the configured threshold changes, when coverage contract fixtures run, then both contributor documents must publish the exact formatted value derived from `line-coverage-gate.json` rather than a separately hard-coded expectation.
- Given the complete Docker-free coverage lane, when union validation runs, then every required project has a report, all three AccessTelemetry assemblies are source-attributed, and the approved 76.5% threshold passes.

## Spec Change Log

- 2026-07-29: Added the inventory-driven XML collector guard and the missing private collector reference. Focused fixtures, Release build, and 88-test Cobertura collection pass. Complete validation remains open because current-source evidence exposes a frozen-scope conflict rather than a collector failure.
- 2026-07-29: Human approved expanding the assembly/source-scope inventory only for `Hexalith.Memories.AccessTelemetry.Clock` and `Hexalith.Memories.AccessTelemetry.Contracts`; threshold, report inventory, runner, and validator remain frozen.
- 2026-07-29: Registered both approved AccessTelemetry assemblies and exact source prefixes. The unchanged gate now reports 77.78%, so verification remains open without a separately approved coverage increase or threshold disposition.
- 2026-07-29: Human approved one all-operation canonicalization theory in the existing AccessTelemetry contracts checkpoint tests to close the measured 77-line gap without weakening the 78.0% ratchet.
- 2026-07-29: Human approved a versionless, in-memory Clock-host test covering startup, development UTC sources, scoped signing material, and the attestation endpoint; no production, exclusion, or threshold change is allowed.
- 2026-07-29: Human approved registering `Hexalith.Memories.AccessTelemetry` and honestly rebaselining the expanded all-three-assembly inventory to 76.5%; no exclusions, report-inventory changes, runner changes, or validator changes are allowed.
- 2026-07-29 review loop 1: `bad_spec` finding — contributor documentation and its fixture still pinned 78.0% after the approved 76.5% rebaseline. Amended File Scope, tasks, and acceptance to update both documents and derive the fixture expectation from configuration, avoiding a green test that preserves stale guidance. KEEP: versionless private collector; inventory-driven collector guard; all three exact AccessTelemetry source mappings; seven-project report inventory; no new exclusions; unchanged runner/validator; approved 76.5% threshold; all-operation canonicalization theory; real in-memory Clock-host signature proof; previously measured 108/108 and 76.70% evidence.
- 2026-07-29 review loop 1 patch triage: section-bound the derived documentation threshold assertions; rejected conditional, updated, removed, excluded, or locally version-overridden collector declarations; isolated the Clock host from ambient configured sources while retaining Program's three real development registrations; corrected clean-build and unstaged whitespace commands; and recorded the pre-existing six-vs-seven contributor inventory wording in `deferred-work.md`.

## Design Notes

The fixture should parse MSBuild XML and map each configured project name to
`tests/<project>/<project>.csproj`. It should require exactly one
`PackageReference` whose `Include` is `coverlet.collector`, `PrivateAssets` is
`all`, and `IncludeAssets` matches the established sibling-project value. This
binds the preventive check to the same inventory the runtime validator uses.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/coverage_gate -p "*_test.py"` -- expected: all coverage contract and validator fixtures pass.
- `dotnet build tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj --configuration Release` -- expected: zero warnings and errors.
- `dotnet test tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj --configuration Release --no-build --filter "Category!=Integration&Category!=Benchmark" --collect "XPlat Code Coverage" --settings tests/tests.runsettings --results-directory TestResults/access-telemetry-coverage` -- expected: all tests pass and a Cobertura attachment exists.
- `dotnet build Hexalith.Memories.slnx --configuration Release && bash ./tools/test.sh --filter "Category!=Integration" --configuration Release --no-build --coverage --results-directory TestResults/test-unit-contract && python3 tools/validate-coverage.py --results-directory TestResults/test-unit-contract --config tests/tooling/coverage_gate/line-coverage-gate.json` -- expected: all tests pass, all required reports exist, and coverage is at least 76.5%.
- `git diff --check && { git diff --no-index --check /dev/null _bmad-output/implementation-artifacts/spec-gh-30423676094-access-telemetry-coverage-collector.md >/dev/null; result=$?; [ "$result" -le 1 ]; }` -- expected: no whitespace errors in tracked or untracked changes.

**Executed evidence (2026-07-29):**

- RED before the project repair: `python3 tests/tooling/coverage_gate/coverage_contract_test.py CoverageContractTests.test_required_report_projects_reference_the_private_coverlet_collector` exited `1`; the failure named `Hexalith.Memories.AccessTelemetry.Tests` and reported zero collector references.
- GREEN after the project repair: the same focused command passed `1/1`; `python3 -m unittest discover -s tests/tooling/coverage_gate -p "*_test.py"` passed `32/32`.
- `dotnet build tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj --configuration Release` succeeded with `0` warnings and `0` errors.
- The focused `dotnet test ... --collect "XPlat Code Coverage"` command passed `88/88` with no failures or skips and emitted `TestResults/access-telemetry-coverage/d3b9e5f7-b6b0-4a68-80d0-fac20ad2fcc5/coverage.cobertura.xml`.
- `dotnet restore Hexalith.Memories.slnx -p:Configuration=Release -p:WarningsNotAsErrors=NU1901%3BNU1902%3BNU1903%3BNU1904 && dotnet build Hexalith.Memories.slnx --configuration Release --no-restore -m:1` succeeded with `0` warnings and `0` errors.
- The exact complete wrapper command stopped in `Hexalith.Memories.Server.Tests` with `2` unrelated repository-root discovery failures because the isolated worktree intentionally has only the two task-authorized root submodules initialized. After collecting the remaining required project reports individually, the unchanged validator initially exited `1` with `in-scope assembly 'Hexalith.Memories.AccessTelemetry.Clock' has no declared source scope`; the same report also contained `Hexalith.Memories.AccessTelemetry.Contracts`. That change was held at **Ask First** until the later human approval recorded above.
- Matrix audit: focused real-project RED/GREEN covers collector drift and AccessTelemetry emission; `ValidateCoverageTests.test_validator_requires_a_nonempty_report_from_every_inventory_project` ran in the 32-test fixture suite and covers missing or empty runtime reports.
- After the approved source-scope registration, `python3 -m unittest discover -s tests/tooling/coverage_gate -p '*_test.py'` initially ran 32 tests and exited `1`: the sole failure was the existing `test_configuration_pins_threshold_scope_and_required_evidence` expected list. After separate approval to align that guard with exactly the same two assemblies, the command passed all `32/32` tests.
- `python3 tools/validate-coverage.py --results-directory TestResults/test-unit-contract --config tests/tooling/coverage_gate/line-coverage-gate.json` processed 7 unique reports plus 7 duplicate attachments and exited `1` at **28,168/36,213 = 77.78%**, below the unchanged **78.00%** minimum. Both new scopes were attributed: Clock **118/204 = 57.84%** and Contracts **552/614 = 89.90%**.
- The approved `Canonicalizer_SupportedOperationTuple_RoundTrips` theory covers all nine `ok` tuples, all nine matching `error` tuples, and the supported `search`/`partial` tuple. Its delete rows exercise both non-tenant case-required and tenant case-forbidden query shapes; every row parses and recanonicalizes byte-for-byte without production or gate changes.
- `dotnet build tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj --configuration Release` succeeded with `0` warnings and `0` errors. The focused Release coverage command passed `107/107` tests with no failures or skips and emitted `TestResults/access-telemetry-operation-coverage/2a9180f4-d350-4459-9e10-ec353672d28b/coverage.cobertura.xml`.
- The unchanged validator over a combined seven-project evidence directory containing that fresh report and the six previously collected current-source reports processed 7 unique reports and exited `1` at **28,170/36,213 = 77.79%**, below the unchanged **78.00%** minimum. Contracts increased to **554/614 = 90.23%** while Clock remained **118/204 = 57.84%**; the approved behavior theory adds two covered production lines but does not close the ratchet.
- `python3 -m unittest discover -s tests/tooling/coverage_gate -p '*_test.py'` passed all `32/32` fixtures after the behavior-theory implementation.
- The approved in-memory Clock-host test boots the real Clock entry assembly in `Development`, registers exactly `development-utc-a`, `development-utc-b`, and `development-utc-c`, resolves a scoped PKCS#8 P-256 signing key from a substituted `DaprClient`, posts to `/v1/time/attest`, and verifies the returned signature with `ClockAttestationVerifier`. The focused direct xUnit command passed `1/1` after an initial configuration-order red proved the sources must be supplied through early host settings.
- After adding the versionless centrally pinned `Microsoft.AspNetCore.Mvc.Testing` reference, the Release project build succeeded with `0` warnings and `0` errors. The fresh Release coverage command passed `108/108` tests with no failures or skips and emitted `TestResults/access-telemetry-clock-host-coverage/77aee043-7280-4288-a1b8-c67c5aa6ca12/coverage.cobertura.xml`.
- That fresh report records Clock **185/204 = 90.69%** and Contracts **535/614 = 87.13%**, but it also truthfully emits `Hexalith.Memories.AccessTelemetry` at **802/1,648 = 48.67%**. The unchanged validator over this report plus the six current-source required-project reports exits `1` with `in-scope assembly 'Hexalith.Memories.AccessTelemetry' has no declared source scope` before producing a union percentage. Adding or excluding that assembly is outside the approved scope, so verification remains open.
- `python3 -m unittest discover -s tests/tooling/coverage_gate -p '*_test.py'` passed all `32/32` fixtures after the Clock-host implementation.
- After the final human-approved policy decision, the configuration and pinned fixture register `Hexalith.Memories.AccessTelemetry` with exact source prefix `src/Hexalith.Memories.AccessTelemetry` and set `minimumLineCoveragePercent` to exactly `76.5`; Clock/Contracts mappings, all seven `requiredReportProjects`, exclusions, runner, and validator remain unchanged. The coverage fixture command passed all `32/32` tests.
- The unchanged validator over `TestResults/test-unit-contract-clock-host-coverage` processed 7 unique reports with 0 duplicate attachments and passed at **29,039/37,861 = 76.70%**, above the approved **76.50%** minimum. Assembly matrix: AccessTelemetry **802/1,648 = 48.67%**; AccessTelemetry.Clock **185/204 = 90.69%**; AccessTelemetry.Contracts **554/614 = 90.23%**; CLI **3,365/4,173 = 80.64%**; Client.Rest **547/606 = 90.26%**; Contracts **1,302/1,325 = 98.26%**; EventStore **462/1,186 = 38.95%**; MCP **725/793 = 91.42%**; Server **17,784/23,843 = 74.59%**; ServiceDefaults **325/375 = 86.67%**; Telemetry **96/103 = 93.20%**; Web **2,892/2,991 = 96.69%**. The required CLI, MCP, and Server `Program.cs` roots retain 49, 10, and 56 executable lines respectively.
- Review-loop iteration 1 re-derived the implementation after the tracked reset and preserved every KEEP item. The Release AccessTelemetry project build completed with **0 warnings and 0 errors**; the configuration-driven tooling suite passed **32/32**; focused Release coverage passed **108/108** and emitted `TestResults/review-loop-1-access-telemetry-coverage/2243b0dc-2980-49cd-839a-b2b78605c818/coverage.cobertura.xml`.
- The real in-memory Clock-host proof passed **1/1** while an ambient `APP_API_TOKEN` was present, without mutating process environment configuration. It booted the real Clock entry assembly in `Development`, returned HTTP 200 from `/v1/time/attest`, exposed the exact three development sources, and verified the returned signature with `ClockAttestationVerifier`.
- Fresh reports were emitted for all seven configured report projects under `TestResults/review-loop-1-seven-project-coverage`. The six additional project runs recorded Contracts **589 passed**; CLI **489 passed**; MCP **107 passed**; EventStore **129 passed**; Web **492 passed**; Server **2,747 passed, 1 skipped, 2 failed**. The two Server failures are isolated-worktree repository-root discovery tests (`DirectoryBuildProps_CheckSubmodulesUsesItemDrivenErrorMessage` and `DirectoryBuildProps_CheckSubmodulesIncludesEveryGitmodulePath`); they walk up from the test output looking for a `.git` directory, while a Git worktree has a `.git` file. They are unrelated to the coverage change and did not prevent fresh report emission.
- `python3 tools/validate-coverage.py --results-directory TestResults/review-loop-1-seven-project-coverage --config tests/tooling/coverage_gate/line-coverage-gate.json` processed exactly **7 unique reports, 0 duplicates** and passed at **29,039/37,861 = 76.70%**, above **76.50%**. The matrix and required source-root counts exactly match the approved earlier evidence.
- `tests/README.md` and `CONTRIBUTING.md` now publish **76.5%**. Their coverage-contract assertions format `minimumLineCoveragePercent` from `line-coverage-gate.json`; no separate documentation threshold literal remains in the fixture.
- Final controls passed: JSON parse, `git diff --check`, explicit story file-scope validation for all eight changed paths, unchanged `requiredReportProjects` and `excludePathGlobs`, and a forbidden-path audit. The final tracked diff contains only the seven approved implementation/documentation files; no production, workflow, package catalog, runner, validator, report inventory, exclusion, submodule, or `_bmad/render` file changed.
- Post-review patch checks passed: all **32/32** coverage tooling fixtures; the in-memory Clock-host proof **1/1** with injected ambient `Clock__Sources__0__*` and the exact three development-source IDs retained; and repository-policy line endings for every changed Markdown, C#, project, JSON, and Python file.
- The exact acceptance sequence ran in a disposable normal Git clone with a real `.git` directory: the Release solution build succeeded with **0 warnings and 0 errors**; the wrapper passed Contracts **589**, Server **2,749** with **1 expected skip**, AccessTelemetry **108**, CLI **489**, MCP **107**, EventStore **129**, and Web **492** — **4,663 passed, 1 skipped, 0 failed** total. The unchanged validator processed all seven required reports and passed at **29,037/37,861 = 76.69%**, above the approved **76.50%** minimum; this supersedes the linked-worktree-only Server failures recorded above.

## Suggested Review Order

**Coverage gate**

- Start with the approved inventory and honest ratchet that govern the complete lane.
  [`line-coverage-gate.json:2`](../../tests/tooling/coverage_gate/line-coverage-gate.json#L2)

- Review the configuration-driven collector guard and fail-fast metadata constraints.
  [`coverage_contract_test.py:82`](../../tests/tooling/coverage_gate/coverage_contract_test.py#L82)

- Confirm both centrally versioned test dependencies remain private and versionless.
  [`Hexalith.Memories.AccessTelemetry.Tests.csproj:21`](../../tests/Hexalith.Memories.AccessTelemetry.Tests/Hexalith.Memories.AccessTelemetry.Tests.csproj#L21)

**Behavioral proof**

- Verify real Clock startup, isolated development sources, signing, HTTP, and production verification.
  [`ClockAttestationCheckpointTests.cs:57`](../../tests/Hexalith.Memories.AccessTelemetry.Tests/Clock/ClockAttestationCheckpointTests.cs#L57)

- Verify every supported operation/outcome tuple round-trips through the canonical contract.
  [`AccessTelemetryContractsCheckpointTests.cs:153`](../../tests/Hexalith.Memories.AccessTelemetry.Tests/Contracts/AccessTelemetryContractsCheckpointTests.cs#L153)

**Contributor contract**

- Confirm documentation assertions derive and section-bind the configured threshold.
  [`coverage_contract_test.py:303`](../../tests/tooling/coverage_gate/coverage_contract_test.py#L303)

- Check local test guidance publishes the same authoritative ratchet.
  [`README.md:77`](../../tests/README.md#L77)

- Check contributor workflow guidance matches the enforced CI behavior.
  [`CONTRIBUTING.md:338`](../../CONTRIBUTING.md#L338)

**Review follow-up**

- Track the pre-existing six-vs-seven documentation inventory wording separately.
  [`deferred-work.md:2590`](deferred-work.md#L2590)
