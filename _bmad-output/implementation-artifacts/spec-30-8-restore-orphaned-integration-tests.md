---
title: 'Restore the orphaned Integration tests and guard CI lane selection'
type: 'bugfix'
created: '2026-11-16'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '30-8-restore-orphaned-integration-tests'
baseline_commit: 'dd422905e563d0c23b7232df910a0c41d82d7b03'
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-30-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Retiring `tools/test-release.ps1` (30.3 `8aee7a13`, 30.6 `e1cded9f`) left the eight `[Trait("Category", "Integration")]` Kreuzberg cases in `ContentExtractionClientTests` in no lane (retro F11, A3, item 93). An xUnit-discovery diff over all ten built test projects confirms they are the only tests the retired runner ran that today's lanes skip. It also finds tests no lane ever selected: three Integration+Performance latency tests and 60 untagged tests in eight `IntegrationTests` classes.

**Approach:**
- Remove the trait from the eight cases. They are hermetic, so `test-unit-contract` runs them.
- Add `IntegrationSlow` to the three latency tests, so nightly `integration-slow` runs them like the two existing Performance tests.
- Register the 60 untagged tests in deferred-work with a trigger.
- Add `CiTestInventoryTests` facts that model the `tools/test.sh` lanes and the source traits. One asserts that every Integration-tagged test is selected by an automatic lane. The other asserts that the tests no lane selects equal a reviewed register.

## Boundaries & Constraints

**Always:**
- The lane model mirrors `tools/test.sh`:
  - The filter picks the inventory.
  - `Category!=Integration` also excludes `Benchmark`.
  - `Category=Benchmark` runs unfiltered.
  - Trait semantics follow Microsoft.Testing.Platform: a test passes `--filter-trait` when it matches any value, and `--filter-not-trait` when it matches none.
  - The fact asserts the `test.sh` routing text it mirrors, so drift in `test.sh` fails it.
- A lane counts only when its workflow has a `push`, `pull_request` or `schedule` trigger, and neither its job nor its step has an `if:`.
- Fail loudly on unmodelled shapes:
  - `test.sh` without `--filter`, or an unmodelled filter;
  - `tools/test.ps1` in a workflow;
  - a Fact, Theory or Trait attribute at an unmodelled indent;
  - a trait on a nested class.
- Red first: the Integration fact runs red on the baseline tree with exactly 11 orphans, and that run is recorded before any test file changes.
- Register Story 30.8 in `epics.md`, `sprint-status.yaml` and `epic-30-context.md` with minimal additive diffs. Never run `sprint_status.py`.

**Never:**
- Change `ci.yml`, `nightly.yml`, `test.sh`, `test.ps1` or any inventory file.
- Tag or fix the 60 untagged tests, including the failing `ReferencedSourceProducerIntegrationTests` case.
- Work on A4–A11 (items 94–101), dispatch a release or recovery, touch the wall-clock budget tests, or flip the retro verdict.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Baseline tree | 8 Kreuzberg and 3 latency tests unselected | The Integration fact fails, naming each `file:line`, test and categories | N/A |
| After the change | Retag, `IntegrationSlow` added, register written | Both facts pass; `test-unit-contract` selects the eight | N/A |
| New Integration test in a unit-contract project | No lane selects it | The Integration fact fails | N/A |
| New untagged test in `IntegrationTests` | A register count no longer matches | The register fact fails | N/A |
| A registered orphan gets tagged | The count drops | The register fact fails until the register shrinks | N/A |
| Unmodelled lane or attribute shape | For example, `test.sh` without `--filter` | Fails loudly, naming the workflow, step or file line | Fail closed |

</frozen-after-approval>

## Code Map

- **`tests/Hexalith.Memories.Server.Tests/Ingestion/ContentExtractionClientTests.cs`** -- traits at lines 46, 65, 86, 107, 127, 146, 165 and 182.
  - Story 1.3 added them as a "platform guard" in case CI lacked the native binary.
  - Kreuzberg 4.10.4 bundles `libkreuzberg_ffi.so` and `libpdfium.so` for linux-x64. The `.so` links only libc, libstdc++, libm and libgcc_s. The package also ships native binaries for linux-arm64, both musl targets, osx-arm64 and win-x64.
  - Under `unshare -rn` (no network, no DNS) they pass 8/8 in 2.05 s. The only write is `$TMPDIR/kreuzberg-pdfium/libpdfium.so`.
  - Release run `41448346915` (job `125287006821`, `ad9a5ff6`) ran them on ubuntu-latest. That job ran Server.Tests with only `Category=Benchmark` excluded: 3185 passed, 1 skipped.
- **`tests/Hexalith.Memories.IntegrationTests/Search/`** -- add `[Trait("Category", "IntegrationSlow")]` beside `Performance` at `GraphScopedSearchIntegrationTests.cs:710`, `SemanticSearchIntegrationTests.cs:482` and `SyntacticSearchIntegrationTests.cs:606`.
  - Precedent: `OpenBaoTopologyIntegrationTests.cs:175-176` and `Performance/PipelinePersistencePerformanceTests.cs:31-33`.
  - Locally they pass 3/3 in 9.1 s against Testcontainers Redis. Nightly is green (`43846941740`).
- **Register (source test methods per file).**
  - Orphans to register:
    - `Fixtures/OllamaOidcFakeServerTests.cs` 11 (Aspire fixture)
    - `Fixtures/PublicSurfaceStabilityTests.cs` 5 (no fixture)
    - `Ingestion/InventoryWirePaginationIntegrationTests.cs` 1
    - `Ingestion/MemoryErasureProcessProtocolTests.cs` 5
    - `Ingestion/ReferencedSourceProducerIntegrationTests.cs` 4
    - `Ingestion/SourceInventoryProducerIntegrationTests.cs` 17
    - `Ingestion/TenantIngestionLifecycleIntegrationTests.cs` 5
    - `Ingestion/UnitOwnershipAuthorityIntegrationTests.cs` 12
  - Local run of the six Testcontainers classes: 117/118 cases pass. `ReferencedSourceProducerIntegrationTests` fails non-deterministically: 1 to 3 cases per run, always including `FailedReingestion…(failBeforeScheduling: True, storedOwner: "foreign")`.
  - Unselected by design:
    - `IntegrationTests/Fixtures/MemoryErasureProcessWorker.cs` 1: a pipe-worker entry point that does nothing under discovery.
    - `Cli.Tests/Telemetry/CliTelemetryStartupLatencyBenchmark.cs` 1: an opt-in Benchmark outside the benchmark project.
- **`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`** -- reuse:
  - Workflow parsing: `GetWorkflowJobNames:2039`, `GetWorkflowJobLines:1994`, `GetWorkflowJobScalar:2024`, and `ParseReleaseWorkflowSteps:2091`, which parses every workflow's steps (see `GetFastSelectorOwners:2070`).
  - `ReadInventory:2630` and `GetRepoRoot`.
  - Model the register on `ParkedToolingSuites:31` and its stale-entry checks at `:887-893`.
  - Neighbouring facts: `TestScriptsAndCiWorkflow_ShouldUseSharedProjectInventories:155` and `RetiredReleaseTestScript_ShouldRemainAbsent:277`.
- **Lanes today.**
  - Counted: `ci.yml` `test-unit-contract:457` and `integration-fast:596`; `nightly.yml` `integration-slow:111` and `benchmark:162`.
  - `nightly.yml` `integration-fast:59` is dispatch-only, so it does not count.
  - `shared-ci` (Contracts.Tests through Builds) and `source-debug` (one class) are not `test.sh` lanes and select no Integration test.
  - `test.sh` is LF (`*.sh eol=lf`); normalize CR before matching.
- **Test sources.**
  - File-scoped namespaces only. Class attributes sit at column 0 and member attributes at column 4.
  - The only custom fact attribute is `RunnableSkippedFactAttribute`.
  - `DerivedStores.Tests` has no sources, and `Web.E2E` is Playwright.
- **Baseline discovery** (`-list full`, all projects Release-built at `dd422905`).
  - The retired runner selects 3864 cases and `test-unit-contract` 3856. `integration-fast` selects 377, `integration-slow` 37 and `benchmark` 17.
  - In push CI `43862752990` TRX, Server.Tests has 3 `ContentExtractionClientTests` cases, and integration-fast has none from the eight classes.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
  - Add the lane model and a source scanner. Effective categories are class traits plus method traits.
  - Add `IntegrationTraits_EveryTaggedTestIsSelectedByAnAutomaticCiLane`, the register fact `UnselectedTests_MatchTheReviewedRegister` (exact per-file counts, each entry pointing at its DW entry or design reason), and a selection-semantics theory.
  - Add non-vacuity checks: each of the three inventories has an automatic lane, and Integration-tagged tests exist.
  - Run the Integration fact red on the baseline and record it. -- A3 guard.
- [x] `tests/Hexalith.Memories.Server.Tests/Ingestion/ContentExtractionClientTests.cs` -- remove the eight traits -- F11 fix (option a).
- [x] The three `IntegrationTests/Search/*SearchIntegrationTests.cs` files -- add `IntegrationSlow` -- gives the latency orphans a lane.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` -- add one entry for the 60 untagged tests. Owner Murat. Reopen trigger: the next story that touches one of the eight files or the integration-fast filter. Record the evidence, including the failing case.
- [x] `_bmad-output/planning-artifacts/epics.md`, `sprint-status.yaml`, `epic-30-context.md` -- register Story 30.8 (row, execution order, checkpoint table).
- [x] After green push CI on the exact final commit, set item 93 and the 30.8 row to done, citing the run and Server.Tests TRX counts.

**Acceptance Criteria:**
- Given the baseline tree, when the Integration fact runs, then it fails naming exactly the 8 Kreuzberg and 3 latency tests. After the change it passes, and so does every `CiTestInventoryTests` case.
- Given the final commit, when push CI runs, then all checks succeed. The `test-unit-contract` Server.Tests TRX shows all 11 `ContentExtractionClientTests` cases executed and passed. Item 93 closes only after that, citing the run and its TRX counts.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Release/net11.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` -- expected: 0 failed. On the baseline, the new Integration fact fails with 11 orphans.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net11.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Ingestion.ContentExtractionClientTests -trait- Category=Integration -trait- Category=Benchmark` -- expected: 11 passed.
- Re-run the xUnit-discovery diff: the retired-minus-current set is empty, and the unselected set equals the register.
- `gh run list --commit $(git rev-parse HEAD)` -- expected: push CI `success`.

## Implementation Notes

- **Baseline red recorded 2026-11-16 before changing the eight Kreuzberg or three latency traits.** Baseline source `dd422905e563d0c23b7232df910a0c41d82d7b03`; only the new inventory guard was compiled. Under `run-with-temp bash -c`, ran `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release --artifacts-path "$TMPDIR/build" -m:1`, then `DiffEngine_Disabled=true dotnet exec "$TMPDIR/build/bin/Hexalith.Memories.Cli.Tests/release/Hexalith.Memories.Cli.Tests.dll" -method Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests.IntegrationTraits_EveryTaggedTestIsSelectedByAnAutomaticCiLane`. Build: zero warnings/errors. Fact: 1 failed, zero skipped, naming exactly 11 orphans: the eight `ContentExtractionClientTests` methods and the three Search latency methods. Retained logs: `artifacts/story-30-8/baseline-build.log` and `artifacts/story-30-8/baseline-red.log`.


- **Local green verification (2026-11-16).** `run-with-temp python3 artifacts/story-30-8/verify-local.py` built Cli.Tests, Server.Tests and IntegrationTests sequentially with `dotnet build <project> --configuration Release --artifacts-path "$TMPDIR/build" -m:1`: all three builds had zero warnings/errors. The script retained exact expanded commands in `artifacts/story-30-8/verification.log` and per-command logs. It then ran:
  - `DiffEngine_Disabled=true dotnet exec "$TMPDIR/build/bin/Hexalith.Memories.Cli.Tests/release/Hexalith.Memories.Cli.Tests.dll" -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`: **181 passed, zero failed/skipped**, 1.542 s (`local-green.log`).
  - `DiffEngine_Disabled=true dotnet exec "$TMPDIR/build/bin/Hexalith.Memories.Server.Tests/release/Hexalith.Memories.Server.Tests.dll" -class Hexalith.Memories.Server.Tests.Ingestion.ContentExtractionClientTests -trait- Category=Integration -trait- Category=Benchmark`: **11 passed, zero failed/skipped**, 0.243 s (`extraction-green.log`), proving the eight restored methods pass the unit-contract exclusions.
  - `DiffEngine_Disabled=true dotnet exec "$TMPDIR/build/bin/Hexalith.Memories.IntegrationTests/release/Hexalith.Memories.IntegrationTests.dll" -class Hexalith.Memories.IntegrationTests.Search.GraphScopedSearchIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.SemanticSearchIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.SyntacticSearchIntegrationTests -trait Category=Performance`: **3 passed, zero failed/skipped**, 9.726 s (`latency-green.log`), against the existing Testcontainers fixtures.
- **Discovery diff.** The same script invoked `dotnet exec <assembly> -list full/json` (the machine-readable form of `-list full`) on all ten inventoried test assemblies: the three just built plus the seven existing baseline assemblies. **4,363 discovered records; retired runner selects 3,867, automatic current lanes select 4,301; retired-minus-current is empty.** The additional three retired-selected source discovery records versus baseline are the two new facts and selection theory. Exactly **62 records remain unselected**: the eight DW-731 files have counts 11/5/1/5/4/17/5/12 (60 total), plus one inert pipe worker and one opt-in CLI benchmark. `artifacts/story-30-8/discovery-diff.json` and `discovery-*.json` retain the result and raw inventory.
- **Edge-case matrix and fail-closed verification.** `run-with-temp python3 artifacts/story-30-8/verify-mutations.py` copied sources, configuration, workflows and inventories beneath its private `$TMPDIR`, built the guard there, and changed only that disposable copy. **13 expected refusal runs and four passing controls** succeeded (`artifacts/story-30-8/mutations.log` and `mutation-*.log`): new Integration orphan in Server.Tests; a sixth untagged PublicSurface method; a registered PublicSurface method gaining Integration (count drops to four); absent `--filter`; unknown filter; PowerShell runner; Fact, Theory and Trait each at indent eight; a nested-class Trait; conditional lane job; conditional lane step; and Bash routing drift. Each refusal checks the specific diagnostic, and both facts pass before and after restoring fixture bytes.
- **Local delivery checkpoint (before CI).** The eleven source test traits are fixed; the two facts, selection-semantics theory, DW-731 and Story 30.8 registrations are implemented. Existing user changes to `epic-30-context.md` were preserved, with only the story and dependency bullets added. No workflow, runner or inventory changed; no `sprint_status.py`, release, recovery, staging, commit, push or remote operation ran. At that checkpoint Story 30.8 remained `in-progress` and retro item 93 stayed `open` pending green push CI on the exact final commit with **11/11 extraction cases passed in Server.Tests TRX**. The known deferred producer failure and the 60 untagged methods remain unchanged.

- **Independent review and final verification (2026-11-16).** Blind Hunter, Edge Case Hunter and Verification Gap all returned; each finding is recorded below. Eight distinct scanner/model defects were patched within `CiTestInventoryTests.cs`: unsupported qualification and custom attribute definitions, duplicate partial declarations, conditional traits, custom Compile membership, repeated filters, non-executable command text, and runner changes outside the old text snippets. The entire LF-normalized runner is now bound by a documented SHA-256 pin; updating it requires model review. The guard intentionally refuses unsupported forms instead of crediting unproven selection. No public API, dependency, workflow, runner, inventory or deferred test source changed.
- **Review-fix evidence.** The implementation agent ran the two guard facts and selection theory (**15 passed**) plus **16 expected mutation refusals and four positive controls**; see [review-fixes-focused.log](../../artifacts/story-30-8/review-fixes-focused.log) and [review-fixes-mutations.log](../../artifacts/story-30-8/review-fixes-mutations.log). The primary agent then reran `run-with-temp python3 artifacts/story-30-8/verify-local.py` and `run-with-temp python3 artifacts/story-30-8/verify-mutations.py` against the patched tree: all three builds had zero warnings/errors; **181/181 inventory, 11/11 extraction and 3/3 latency cases passed**, zero skips; discovery remains **4,363 records, empty retired-minus-current, exactly 62 reviewed exclusions**; all **13 original mutation refusals and four controls** passed. Total mutation coverage is **29 expected refusals**, with restored controls green. All child work completed and disposable workspaces were removed. `git diff --check` passes.
- **Historical acceptance gate at local review.** Local implementation, independent review and all matrix verification are complete. At that checkpoint this spec was `in-review`, the sprint row was `in-progress`, and item 93 was `open` pending final-commit push CI and its Server.Tests TRX. The build workflow prohibits automatic push, so that remaining acceptance is not claimed. No local commit or push was made while this external gate remains pending.

## Spec Change Log

## Review Triage Log


| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| BH-1 qualified Trait | medium | The detector only permits optional `Xunit.` and skips `global::Xunit.Trait`, treating an excluded Integration method as unit-selected. Patch: recognize or explicitly refuse that qualification. |
| BH-2 custom Fact | medium | The detector enumerates three attribute names; the existing RunnableSkippedFactAttribute proves FactAttribute derivation is supported by this repository, while another derived attribute is invisible. The review mutation omitted its definition; adding the same derivation in a separate file makes the omission reachable. Patch: fail closed on new unsupported custom test attribute definitions. |
| BH-3 split partial-class traits | medium | Class categories reset for each physical declaration, so another partial declaration's Integration trait never reaches the methods. Patch: merge traits by project and fully qualified class, or refuse the unsupported arrangement. |
| BH-4 conditional metadata | medium | The scanner reads all branches and can credit an inactive IntegrationSlow trait on an Integration+Performance method. Existing conditional test bodies do not resolve conditional metadata. Patch: refuse conditional traits rather than model inactive metadata as selection. |
| BH-5 custom Compile membership | medium | Directory enumeration ignores linked Compile Includes and counts removed Compile items. Current inventoried projects use default membership, allowing a small explicit refusal of unsupported membership changes. Patch that boundary without evaluating arbitrary MSBuild. |
| BH-6 repeated filter | medium | Regex.Match takes the first --filter, while test.sh's argument loop replaces FILTER on every occurrence. Patch: require one filter. |
| BH-7 non-executable command text | medium | A substring match credits `echo bash ./tools/test.sh ...` as a lane. Patch: require the supported direct command shape. |
| BH-8 runner drift beyond pinned snippets | medium | Reassigning MTP_FILTER_ARGS after the checked call leaves every pinned substring present while changing emitted arguments. Patch: bind the model to the reviewed normalized runner as a whole. |
| BH-9 same-file identity swap | false | The approved register explicitly uses exact source-method counts per file, not test identities. A simultaneous replacement preserving that count does not violate its specified invariant; no identity register will be introduced. |
| EC-1 qualified Trait | medium | Same verified detector omission as BH-1; group with BH-1 for the patch. |
| EC-2 split partial-class traits | medium | Same verified per-declaration category loss as BH-3; group with BH-3 for the patch. |
| EC-3 repeated filter | medium | Same verified first-versus-last argument mismatch as BH-6; group with BH-6 for the patch. |

Verification-gap reviewer: no verification gaps found. All three independent review layers returned before triage. The eight distinct surviving defects have local corrections with no public API or new dependency; they route to patch. Exact-source push CI was pending at review; it is satisfied by the acceptance evidence below.

Patch disposition: all eight surviving defect groups were fixed and verified by the focused review-fix mutations and final full local checks above. BH-9 was rejected on the per-file-count contract. No new review debt was deferred; DW-731 remains the approved scope debt.

Delivery authorization: 2026-11-16 the user explicitly approved committing and pushing the reviewed Story 30.8 changes so exact-source push CI and Server.Tests TRX can finish acceptance. The earlier no-commit/no-push notes record the local-review checkpoint. Pre-existing unrelated edits to epic-30-context.md will remain outside the task commit.


## Push CI Acceptance — 2026-11-16

Final implementation commit: `898f451111740ccf489d56f7c20cd47edc2fcdf6`. All triggered push workflows succeeded on that exact SHA: [CI 43886197182](https://github.com/Hexalith/Hexalith.Memories/actions/runs/43886197182), [CodeQL 43886197372](https://github.com/Hexalith/Hexalith.Memories/actions/runs/43886197372), and [Commitlint 43886196932](https://github.com/Hexalith/Hexalith.Memories/actions/runs/43886196932). The CI job `test-unit-contract` (`133364878062`) passed, including the normal coverage gate. No release or recovery was dispatched.

The `test-unit-contract-results` artifact (`14235084321`, ZIP SHA-256 `098d5de7887104b2aef86e58b9ff2f6ab663e9c339bee80dd5ed077a9d424d36`) contains `Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.trx`. Parsed definitions and results match all eleven current `ContentExtractionClientTests` method names: **11 executed, 11 passed, 0 failed, 0 skipped**. The entire Server.Tests TRX reports **3,207 total, 3,206 executed/passed, 0 failed, 1 notExecuted**; that separate skipped case is outside the extraction class. Retained evidence: [extraction proof JSON](../../artifacts/story-30-8/ci-extraction-proof.json) and [Server.Tests TRX](../../artifacts/story-30-8/server-tests-ci-43886197182.trx).

Story 30.8 and retro item 93 are done on this evidence. DW-731 remains open for the sixty untagged source methods; A4–A11, the retro verdict, and the wall-clock budget tests remain untouched. The separate records commit writes this acceptance after the implementation CI completed and cannot cite its own future run. The authorized direct push reported `Bypassed rule violations for refs/heads/main` (pull-request and expected-status-check rules). Pre-existing epic-context edits remain unstaged.
