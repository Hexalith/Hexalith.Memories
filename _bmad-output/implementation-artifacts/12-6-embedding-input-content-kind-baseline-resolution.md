# Story 12.6: EmbeddingInputContentKind Baseline Resolution

Status: done

Story Key: 12-6-embedding-input-content-kind-baseline-resolution
Epic: 12 - First Release & Operations Foundation
Created: 2026-05-01

**Effort estimate:** ~0.5-1.5 working days if the current focused green result holds; ~1-2.5 days if a real race or contract drift still reproduces under the full lane.

## Story

As a quality owner,
I want the single tracked baseline filter currently in `tools/test-release.ps1` (`EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag`) to be resolved,
so that the baseline filter list returns to zero and any future addition to it is a deliberate, traceable event rather than quiet drift.

## Acceptance Criteria

1. Given `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` is filtered out of `tools/test-release.ps1`, when the implementation starts, then the developer captures whether the test still fails under a clean Release build and under the release-lane command that currently excludes it.

2. Given the focused test now passes in the current checkout, when the failure no longer reproduces after a clean build, then the story removes the stale filter from `tools/test-release.ps1` instead of preserving a no-longer-needed tolerance.

3. Given the focused test still fails in any authoritative path, when root cause is identified, then the outcome is documented as one of: production telemetry bug, test isolation/race bug, stale build artifact, environment dependency, or incorrect test contract.

4. Given root cause is known, when the team resolves it, then exactly one disposition is applied: fix the production or test code and remove the filter; renegotiate the test contract and remove the filter; formally accept the behavior as architecture with rationale and remove the test/filter; or explicitly skip the test with `[Trait("KnownFailure")]` plus a deferred-work entry as an interim fallback.

5. Given resolution is achieved, then `tools/test-release.ps1` has zero accepted baseline filters for `Hexalith.Memories.Server.Tests`, and no `S11-FA` exclusion remains in any release-lane command.

6. Given `CiTestInventoryTests` guards CI and release inventory behavior, then it asserts that the expected accepted baseline count is zero when no open `S11-FX` deferred entries remain.

7. Given the test concerns telemetry tag emission, then validation includes the focused `EmbeddingInputContentKindTests` class and the stronger existing `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` theory, not only the release script path.

8. Given this is a baseline-resolution story, then it does not change package inventory, semantic-release behavior, branch protection, partial-publish alerting, embedding provider migration, vector dimensions, submodule contents, or unrelated test baselines.

## Tasks / Subtasks

- [x] Task 0 - Reproduce and lock the current baseline evidence (AC: 1, 2, 3)
  - [x] Record current branch, commit, build configuration, and whether the run uses `--no-build`.
  - [x] Run the filtered S11-FA test directly:
    ```powershell
    dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --filter "FullyQualifiedName~EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag" --logger "console;verbosity=minimal"
    ```
  - [x] Run the whole `EmbeddingInputContentKindTests` class.
  - [x] Run the existing stronger telemetry theory:
    ```powershell
    dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --filter "FullyQualifiedName~GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag" --logger "console;verbosity=minimal"
    ```
  - [x] If the focused test only passes with `--no-build`, perform a clean Release build before drawing conclusions.
  - [x] Do not edit submodules or initialize nested submodules.

- [x] Task 1 - Identify whether the filter is stale or still required (AC: 1, 2, 3)
  - [x] Inspect `tools/test-release.ps1` and confirm the only current accepted baseline filter is the `S11-FA` `FullyQualifiedName!~EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` entry.
  - [x] Inspect `_bmad-output/implementation-artifacts/deferred-work.md` and confirm the S11-FA entry still points to this exact test.
  - [x] Inspect `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs` and `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs` before changing either file.
  - [x] Compare the S11-FA test with the stronger `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` theory, which uses a unique tenant id and `ShouldHaveSingleItem()` to avoid static-meter contamination.
  - [x] If the test is green after clean build, treat the filter as stale unless the full release lane proves otherwise.

- [x] Task 2 - Remove the accepted-baseline tolerance or fix the real defect (AC: 2, 3, 4, 5)
  - [x] Preferred path: remove the `S11-FA` project filter from `tools/test-release.ps1` and leave no empty `$projectFilters` map unless tests prove a guard needs to parse an explicitly empty map.
  - [x] If the S11-FA test still fails due to test isolation, rewrite it using the existing stronger pattern: unique tenant id, typed tag extraction, and a single observed measurement for that tenant.
  - [x] If the failure is in production telemetry, fix only the minimal code required around `GenerateEmbeddingActivity`, `EmbeddingInput`, or `MemoriesMeter` and keep the telemetry contract `content_kind in {payload, naturalLanguageDescription}` unchanged unless an explicit contract renegotiation is recorded.
  - [x] Do not change embedding provider defaults, vector dimensions, rate-limit semantics, workflow routing, or natural-language retry behavior.

- [x] Task 3 - Update executable inventory/baseline guardrails (AC: 5, 6)
  - [x] Update `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` so `tools/test-release.ps1` cannot keep hidden accepted filters when no open `S11-FX` entries exist.
  - [x] Assert that the S11-FA filter string is absent.
  - [x] Assert that `test-release.ps1` still drives from `tools/test-projects.unit-contract.txt` and still excludes `Category=Benchmark`.
  - [x] If a future accepted filter syntax remains supported, assert every filter names a deferred-work key and fully qualified test name.

- [x] Task 4 - Close or update deferred-work bookkeeping (AC: 2, 4, 5)
  - [x] If the filter is removed, move the S11-FA entry in `_bmad-output/implementation-artifacts/deferred-work.md` into a closed/resolved section or clearly mark it closed with the validating commit/date.
  - [x] If the test is skipped as an interim fallback, keep S11-FA open and update it with the skip trait, rationale, and re-open trigger.
  - [x] Do not create broad `S11-FX` entries unless new baseline failures are discovered by this story's validation.

- [x] Task 5 - Validate the release lane and close honestly (AC: 1-8)
  - [x] Run focused tests after any code/test change.
  - [x] Run `CiTestInventoryTests`.
  - [x] Run `tools/test-release.ps1` after a Release build so the release-lane path proves it no longer needs the filter:
    ```powershell
    dotnet restore Hexalith.Memories.slnx
    dotnet build Hexalith.Memories.slnx --configuration Release --no-restore
    ./tools/test-release.ps1 -Configuration Release
    ```
  - [x] If full `test-release.ps1` is blocked by environment or runtime, record the exact blocker and at minimum run the Server.Tests project without the S11-FA exclusion plus the CLI inventory test.
  - [x] Update this story's Dev Agent Record with commands, results, changed files, and whether S11-FA is closed or deferred.

## File Scope

Allowed files for this story:

- `tools/test-release.ps1` - UPDATE. Remove the stale S11-FA filter, or keep only a narrow explicitly justified filter if the fallback disposition requires it.
- `_bmad-output/implementation-artifacts/deferred-work.md` - UPDATE only to close or revise the S11-FA entry, or to add a new narrow S11-FX entry if validation discovers a separate baseline.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` - UPDATE to enforce zero expected accepted baseline filters when none are open.
- `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs` - UPDATE only if the S11-FA failure still reproduces and the defect is test isolation or an incorrect test contract.
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs` - UPDATE only if consolidating or strengthening telemetry-tag regression coverage is needed.
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` - UPDATE only if validation proves the production telemetry emission is wrong.
- `src/Hexalith.Memories.Server/Activities/Ingestion/EmbeddingInput.cs` - UPDATE only if validation proves the positional content-kind contract is wrong or underdocumented.
- `src/Hexalith.Memories.Telemetry/MemoriesMeter.cs` - UPDATE only if validation proves the metric tag contract or manifest is wrong.
- `_bmad-output/implementation-artifacts/12-6-embedding-input-content-kind-baseline-resolution.md` - UPDATE Dev Agent Record, validation evidence, and completion notes.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad workflow state transitions.

Read/verify only:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-26.md`
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md`
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-30.md`
- `_bmad-output/implementation-artifacts/9-2-dual-embedding-and-causal-chain-indexing.md`
- `_bmad-output/implementation-artifacts/11-1-github-actions-build-and-test-pipeline.md`
- `_bmad-output/implementation-artifacts/11-2-semantic-release-and-nuget-publishing.md`
- `_bmad-output/implementation-artifacts/12-1-first-release-path-validation.md`
- `_bmad-output/implementation-artifacts/12-2-forbidden-default-tolerances-checklist.md`
- `_bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md`
- `_bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md`
- `_bmad-output/implementation-artifacts/12-5-partial-publish-alerting.md`
- `.github/workflows/ci.yml`
- `tools/test-projects.unit-contract.txt`
- `tools/test.ps1`
- `tools/test.sh`

Forbidden by default:

- `.github/workflows/release.yml`
- `tools/publish-nuget.ps1`
- `tools/pack-release.ps1`
- `tools/validate-release-packages.ps1`
- `tools/release-packages.json`
- package metadata, semantic-release config, branch-protection docs, partial-publish alerting, embedding provider migration files, vector migration tooling, and submodule contents.

If validation shows that solving S11-FA requires changing public telemetry names, package contracts, provider dimensions, workflow history shape, or submodule contents, do not absorb that silently. Record a deferred-work entry or split the work into a dedicated story with a `Scope-Override:` rationale.

## Dev Notes

### Epic Context

Epic 12 turns Epic 11 retrospective findings into release-readiness guardrails. Story 12.6 closes S11-FA, the one known accepted release-lane baseline left after Story 11.x review. Its purpose is narrower than Story 12.4: resolve this specific `EmbeddingInputContentKindTests` exclusion and return the release baseline-filter list to zero.

Do not broaden this story into another baseline sweep. Story 12.4 owns discovering and accounting for additional red tests; Story 12.6 owns final disposition of the already tracked S11-FA item.

### Current Baseline State

Current observed repo state at story creation:

- `tools/test-release.ps1` has a `$projectFilters` map with one entry for `tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj`.
- That entry excludes `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` by `FullyQualifiedName!~...`.
- `_bmad-output/implementation-artifacts/deferred-work.md` tracks the same test as `S11-FA` and says the filter should not stay indefinitely.
- A focused local no-build check on 2026-05-01 passed:
  ```powershell
  dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag" --logger "console;verbosity=minimal"
  ```
  Result: 1 passed, 0 failed.
- A focused local no-build check of the whole `EmbeddingInputContentKindTests` class on 2026-05-01 passed: 7 passed, 0 failed.

Treat those local passes as evidence that the filter may now be stale, not as sufficient implementation completion. The developer still needs a clean build and release-lane validation after removing the filter.

### Telemetry Contract Under Test

`GenerateEmbeddingActivity.RunAsync` emits `MemoriesMeter.EmbeddingApiCalls` with two tags:

- `tenant_id`
- `content_kind`

The allowed `content_kind` values are:

- `payload`
- `naturalLanguageDescription`

`EmbeddingInput.ContentKind` is intentionally a positional record parameter with a default value of `EmbeddingContentKind.Payload`. Do not switch it to property-init syntax; Story 9.2 documented that paused workflow histories carry the earlier `{TenantId, ContentText}` JSON shape and rely on the default value during replay.

### Likely Investigation Path

The current code already contains a stronger test in `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs`:

- `ContentKind_PropagatesToTelemetryTag`
- `[Theory]` over `Payload` and `NaturalLanguageDescription`
- unique tenant id per case
- filters observed measurements by tenant
- asserts exactly one matching measurement

The S11-FA test in `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs` uses a broader capture list and fixed tenant id `t`. If a failure still appears under parallel or full-lane execution, investigate static-meter cross-test contamination or stale build artifacts before changing production telemetry. Prefer consolidating around the stronger unique-tenant pattern over adding sleeps, broad retries, or another release-lane exclusion.

### Previous Story Intelligence

Carry forward these Epic 12 lessons:

- Story 12.1 and Story 12.2 established that tolerant defaults are acceptable only with an explicit recovery path, idempotency proof, or visible signal. A stale test filter is a tolerance without current value and should be removed.
- Story 12.3 treats story file scope as a contract. Do not change release workflow, package publishing, submodules, or provider migration code under this story.
- Story 12.4 owns general baseline accounting. This story should close S11-FA and make zero accepted filters executable, not rediscover every historical red.
- Story 12.5 owns partial-publish alerting. Do not touch `tools/publish-nuget.ps1` or `.github/workflows/release.yml`.

Recent git history before story creation:

- `a6ade7d docs(bmad): create story 12.5 context`
- `77d996c docs(bmad): create story 12.4 context`
- `e7fede7 docs(bmad): create story 12.3 context`
- `d97502a feat: add pre-dev hardening output files for process notes and lessons ledger`
- `018600a fix: update subproject commit reference in Hexalith.EventStore`

The current pre-dev hardening run saw a soft preflight working-tree warning for `Hexalith.EventStore`. Treat it as unrelated and do not stage or modify submodule state.

### Architecture and Project Rules

- .NET 10.0, C# latest, nullable enabled, analyzer-clean builds expected.
- XUnit + Shouldly are the test stack; use Shouldly assertions rather than raw `Assert.*`.
- Keep test method names PascalCase and descriptive.
- Preserve the static `MemoriesMeter` instrument and `MetricTagKeyPolicy` manifest unless the metric contract is proven wrong.
- Do not add package versions in `.csproj`; centralized package management owns versions.
- Do not modify shared submodules without explicit approval.

### Latest Technical Information

Web verification performed on 2026-05-01 using primary sources:

- Microsoft Learn's current VSTest-specific `dotnet test` documentation supports `--filter` expressions, `FullyQualifiedName`, `DisplayName`, and `Category` for xUnit, and the `!~` operator for "not contains." This matches the existing release-lane filter shape and is the right primitive for a narrow one-test exclusion while it exists. Source: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-vstest
- Microsoft Learn's selective unit test documentation confirms xUnit traits can be filtered by `Category`. If the fallback disposition uses `[Trait("KnownFailure", "...")]`, keep the release lane explicit and do not hide it behind broad category exclusion unless the deferred-work entry requires that exact shape. Source: https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests

### Testing Requirements

Minimum validation before review:

```powershell
dotnet restore Hexalith.Memories.slnx
dotnet build Hexalith.Memories.slnx --configuration Release --no-restore
dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~EmbeddingInputContentKindTests" --logger "console;verbosity=minimal"
dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag" --logger "console;verbosity=minimal"
dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~CiTestInventoryTests" --logger "console;verbosity=minimal"
./tools/test-release.ps1 -Configuration Release
```

If `./tools/test-release.ps1` is too slow or blocked in the local environment, record the blocker and run every project command the script would have run without the S11-FA filter. Do not claim the baseline is closed without proving the release-lane path.

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 12 and Story 12.6 acceptance criteria.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-26.md` - Option C and S11-FA scaffold.
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md` - baseline failure pattern and S11-FA origin.
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-30.md` - refreshed baseline-filter carry-forward guidance.
- `_bmad-output/implementation-artifacts/deferred-work.md` - current S11-FA entry.
- `_bmad-output/implementation-artifacts/9-2-dual-embedding-and-causal-chain-indexing.md` - original `EmbeddingInput.ContentKind` and Risk #6 / Risk #17 context.
- `_bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md` - sibling general baseline accounting story.
- `tools/test-release.ps1` - release-lane baseline filter to remove or justify.
- `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs` - S11-FA test file.
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs` - stronger telemetry-tag guard pattern.
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` - production metric emission.
- `src/Hexalith.Memories.Server/Activities/Ingestion/EmbeddingInput.cs` - positional content-kind input contract.
- `src/Hexalith.Memories.Telemetry/MemoriesMeter.cs` - metric name and tag-key manifest.
- Microsoft VSTest `dotnet test` docs: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-vstest
- Microsoft selective unit test docs: https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Pre-dev hardening preflight JSON `_bmad-output/process-notes/predev-preflight-latest.json` reported a soft working-tree warning only: ` M Hexalith.EventStore`.
- Story selection logic chose `12-6-embedding-input-content-kind-baseline-resolution` because `ready_count` was `3`, below target `5`, and this was the first backlog story in sprint-status order.
- Focused no-build validation during story creation showed the S11-FA test currently passes: 1 passed, 0 failed.
- Focused no-build validation during story creation showed the full `EmbeddingInputContentKindTests` class currently passes: 7 passed, 0 failed.
- 2026-05-02 implementation start: branch `main`, commit `9cb9d806f477e4124d8ab5fe412106dc198345d6`, configuration `Release`. Initial S11-FA reproduction did not use `--no-build`.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --filter "FullyQualifiedName~EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag" --logger "console;verbosity=minimal"`: passed 1/1.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --filter "FullyQualifiedName~EmbeddingInputContentKindTests" --logger "console;verbosity=minimal"`: passed 7/7.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --filter "FullyQualifiedName~GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag" --logger "console;verbosity=minimal"`: passed 2/2.
- `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release --filter "FullyQualifiedName~CiTestInventoryTests" --logger "console;verbosity=minimal"` after guardrail edits: passed 12/12.
- `dotnet restore Hexalith.Memories.slnx`: succeeded.
- `dotnet build Hexalith.Memories.slnx --configuration Release --no-restore`: succeeded, 0 warnings, 0 errors.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~EmbeddingInputContentKindTests" --logger "console;verbosity=minimal"`: passed 7/7.
- `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag" --logger "console;verbosity=minimal"`: passed 2/2.
- `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~CiTestInventoryTests" --logger "console;verbosity=minimal"`: passed 12/12.
- `./tools/test-release.ps1 -Configuration Release`: passed without S11-FA exclusion. Results: Contracts 468/468, Server 1543/1543, CLI 334/334, MCP 76/76, EventStore 84/84.

### Implementation Plan

- Treat the clean Release S11-FA pass as a stale-filter disposition rather than changing production telemetry or test behavior.
- Remove the stale `FullyQualifiedName!~EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` release-lane filter and avoid retaining an empty `$projectFilters` map.
- Keep future accepted-filter parsing covered with fixture tests while asserting the real repository currently has zero accepted release-lane baseline filters.
- Close S11-FA in deferred-work bookkeeping after the release lane passes without the exclusion.

### Completion Notes List

- Story context created on 2026-05-01.
- Discovery loaded Epic 12 Story 12.6 planning material, S11-FA deferred-work context, Story 9.2 content-kind/telemetry background, current release-lane filter, current telemetry tests, and prior Epic 12 story artifacts.
- The story treats the current green focused result as a stale-filter signal that still needs clean-build and release-lane proof during implementation.
- No implementation changes were made during story creation; this run only created the ready-for-dev story artifact.
- 2026-05-02 implementation confirmed the S11-FA focused test, full `EmbeddingInputContentKindTests` class, and stronger telemetry theory are green under Release.
- Removed the stale S11-FA release-lane filter from `tools/test-release.ps1`; the script now applies only `Category!=Benchmark`.
- Updated `CiTestInventoryTests` to assert zero real accepted release-lane baseline filters, absence of the S11-FA filter string, continued shared inventory usage, continued benchmark exclusion, and valid parsing for any future keyed accepted-filter fixture.
- Moved S11-FA from open deferred work into a closed Story 12.6 section after release-lane validation passed without the exclusion.

### File List

- `_bmad-output/implementation-artifacts/12-6-embedding-input-content-kind-baseline-resolution.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `tools/test-release.ps1`

### Change Log

- 2026-05-01: Created Story 12.6 and promoted it from `backlog` to `ready-for-dev`.
- 2026-05-02: Removed stale S11-FA release-lane baseline filter, closed S11-FA deferred-work entry, added zero-baseline guardrail assertions, and validated the Release lane green without the exclusion.
- 2026-05-02: Code review applied 2 patches to `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` — removed two over-restrictive `ShouldNotContain` substring assertions (lines 83-84) and two unconditional `ShouldBeEmpty` assertions (lines 104-105) that were stricter than AC #6. The existing conditional guard (lines 112-115) and pairing logic (lines 107-110) continue to enforce zero baseline filters when no S11-F* entries are open. `CiTestInventoryTests` 12/12 green after patches. 5 follow-ups deferred (12.6-RV1..12.6-RV5).

## Story Completion Status

Implementation and code review complete. S11-FA is closed, accepted release-lane baseline filters are zero, all tasks/subtasks are checked, code review patches applied (CiTestInventoryTests 12/12 green), and status is set to `done`. 5 follow-ups deferred (12.6-RV1..12.6-RV5).

### Review Findings

- [x] [Review][Defer] Underlying telemetry test `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` still uses fixed tenant id `"t"` and a non-thread-safe capture list against the static `MemoriesMeter.EmbeddingApiCalls` counter — the flake mode that originally motivated S11-FA is dormant, not eliminated. Story File Scope forbids editing this test unless S11-FA reproduces (it didn't); reopening scope mid-review would violate the same File Scope discipline Story 12.3 established. Tracked as 12.6-RV5; the stronger sibling theory `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` remains as primary regression coverage. [tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs:84-119] — deferred, scope-respecting follow-up
- [x] [Review][Patch] Removed two `ShouldNotContain` substring assertions (`EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` and `FullyQualifiedName!~`). Duplicated the parser-based `ReadAcceptedReleaseFilters_RealRepo_HasNoAcceptedBaselineFilters` test and would trip on harmless comments or any future legitimate narrow exclusion. [tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:83-84] — applied
- [x] [Review][Patch] Removed the two unconditional `ShouldBeEmpty` assertions in `TestReleaseBaselineFilters_ShouldMatchOpenDeferredWorkEntries`. AC #6 says "when no open S11-FX entries remain"; the existing conditional guard at lines 112-115 and the pairing logic already enforce exactly that. The unconditional version pinned the test to a one-time snapshot and made the downstream `ShouldAllBe` cross-checks unreachable. [tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:104-105] — applied
- [x] [Review][Defer] Real-repo positive parser canary lost — both real-repo tests now expect empty; only fixture tests prove `ReadOpenDeferredBaselines` parses real-file shapes. Tracked as 12.6-RV1. [tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:217] — deferred, follow-up improvement
- [x] [Review][Defer] S11-FD wording fragility: changing "release pipeline" to "release lane" in `deferred-work.md` would silently flip its baseline classification (12.4-RV6 surface realized by new `ShouldBeEmpty`). Tracked as 12.6-RV2. [tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:372-377] — deferred, structural classifier hardening
- [x] [Review][Defer] New `ReadAcceptedReleaseFilters_ValidKeyedFilter_ReturnsFilter` fixture is single-item; `ShouldHaveSingleItem()` would mask parser over-matching. Tracked as 12.6-RV3. [tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:198-214] — deferred, fixture strengthening
- [x] [Review][Defer] Discoverability breadcrumb removed from `tools/test-release.ps1` — consider a one-line trailing comment pointing at `deferred-work.md` so future maintainers can trace baseline-filter policy. Tracked as 12.6-RV4. [tools/test-release.ps1:25] — deferred, optional comment
