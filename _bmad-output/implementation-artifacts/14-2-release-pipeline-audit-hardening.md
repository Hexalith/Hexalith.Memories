# Story 14.2: Release Pipeline Audit Hardening

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a release maintainer,
I want release workflow and package validation guardrails strengthened,
so that package publication, stale tags, release evidence, and package inventory drift are caught before they can create ambiguous release states.

## Acceptance Criteria

1. Given release workflow hardening is applied, when `.github/workflows/release.yml` is reviewed, then action pinning, stale-tag handling, and partial-publish signal behavior are explicitly decided and either implemented or documented with a new defer-by date.

2. Given package validation runs, when `tools/validate-release-packages.ps1` scans `src/**/*.csproj`, then every packable and non-packable project is accounted for in release package inventory and direct operator version inputs with build metadata fail or normalize with a clear message.

3. Given release evidence is collected, when `docs/dev/release-runbook.md` is updated, then package evidence includes checksum or equivalent audit evidence for newly validated packages and the release bot identity is pinned enough for future forensic review.

4. Given `tools/release-packages.json` is edited, when validation runs, then schema validation or a schema reference catches misspelled package fields before publish-time scripts run.

5. Given CI inventory tests guard release lanes, when workflow text is parsed, then tests avoid broad substring matching where a structural or narrower assertion is feasible.

## Tasks / Subtasks

- [x] Task 1 - Audit and harden the release workflow decisions (AC: 1)
  - [x] Review `.github/workflows/release.yml` action references. Either SHA-pin third-party actions used by the release workflow or document the explicit accepted risk with a fresh defer-by date in `_bmad-output/implementation-artifacts/deferred-work.md`.
  - [x] Reassess stale-tag handling for `.releaserc.json` `tagFormat: "v${version}"`. Implement a bounded preflight if practical, or carry forward `S11-FC` with a concrete trigger and defer-by date.
  - [x] Reassess partial-publish signaling against the current `Alert partial NuGet publish` step, `tools/publish-nuget.ps1`, and `tools/create-partial-publish-issue.ps1`. Mark `S11-FD` resolved only if the issue/comment path is covered by tests and the runbook explains operator recovery.
  - [x] Preserve release job permissions at least privilege. Do not add new write scopes unless the implementation explains why the existing `contents`, `issues`, and `pull-requests` permissions are insufficient.

- [x] Task 2 - Make package inventory validation complete and schema-backed (AC: 2, 4)
  - [x] Update `tools/validate-release-packages.ps1` so every `src/**/*.csproj` is in exactly one inventory bucket: `packages` for `<IsPackable>true</IsPackable>` projects or `nonPackableProjects` for `<IsPackable>false</IsPackable>` projects.
  - [x] Fail loudly when a project has missing, blank, or unsupported `IsPackable` instead of treating it as an implicit non-package.
  - [x] Add a JSON schema or inline schema validation for `tools/release-packages.json`; catch misspelled fields such as `packageID`, `projectPath`, or `nonPackableProject` before any pack or publish script runs.
  - [x] Keep package ID casing checks case-sensitive. NuGet locks first-published package ID casing, so validation must keep rejecting casing drift.
  - [x] Add tests or script fixtures for missing inventory entries, unexpected inventory entries, misspelled JSON fields, duplicate project paths, duplicate package IDs, and missing non-packable projects.

- [x] Task 3 - Normalize or reject build metadata in direct validation inputs (AC: 2)
  - [x] Decide whether `-Version 1.2.3+local` should be rejected or normalized to `1.2.3` inside `tools/validate-release-packages.ps1`.
  - [x] If normalizing, emit a clear message naming both the supplied version and the NuGet-comparable version. If rejecting, fail before package metadata comparison with a clear message.
  - [x] Keep `tools/pack-release.ps1` compatible with the chosen validation behavior; do not let semantic-release versions silently diverge from NuGet package versions.
  - [x] Add regression coverage for the selected build-metadata behavior.

- [x] Task 4 - Improve package audit evidence and release runbook forensic clarity (AC: 3)
  - [x] Update `docs/dev/release-runbook.md` so the package evidence procedure records SHA-256 checksums, `dotnet nuget verify --all`, `nuget verify -Signatures`, or another explicit audit-equivalent result for release assets.
  - [x] If using checksums, document deterministic commands for computing them on Windows and Linux runners.
  - [x] Pin the semantic-release bot identity beyond the display name recorded today. At minimum, document which token identity creates tags/releases in this repository and what evidence future reviewers should capture from GitHub Actions or GitHub Release metadata.
  - [x] Keep the runbook clear that package publishing is CI-only and that operators must not manually run `dotnet nuget push` from a workstation.

- [x] Task 5 - Tighten release-lane test inventory assertions (AC: 5)
  - [x] Replace broad substring checks in release-lane inventory tests with narrower assertions where feasible, especially around `.github/workflows/release.yml`, `tools/test-release.ps1`, and package validation invocation.
  - [x] Prefer YAML-aware or line/step-scoped assertions for workflow checks if the existing test stack supports it without a new dependency. If not, use local helper parsing that anchors to step names and command lines instead of raw file-wide `Contains`.
  - [x] Keep `CiTestInventoryTests.TestReleaseBaselineFilters_ShouldMatchOpenDeferredWorkEntries` behavior intact unless Story 14.5 changes the deferred-work schema.

- [x] Task 6 - Update deferred-work bookkeeping without losing evidence (AC: 1-5)
  - [x] Resolve, remove, or explicitly carry forward `W1`, `W2`, `W12`, `W15`, `W16`, `W19`, `S11-FC`, `S11-FD`, `12.1-RV1`, and `12.1-RV2` according to the implementation result.
  - [x] Do not close an entry solely because the story discussed it. Closure requires code, test, or runbook evidence in the story completion notes.
  - [x] Leave `12.1-RV3`, `12.1-RV4`, and `12.1-RV5` out of scope unless touched incidentally by the same runbook edits and explicitly justified.

- [x] Task 7 - Validate the release audit lane (AC: 1-5)
  - [x] Run `pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1`.
  - [x] Run validation after the focused fixture suite to confirm no temporary package-inventory sentinel projects remain: `pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1`.
  - [x] Run focused tests for any new release-package validation test harness.
  - [x] Run the focused C# CI inventory tests when `CiTestInventoryTests.cs` changes.
  - [x] Run `git diff --check -- .github/workflows/release.yml .releaserc.json tools/validate-release-packages.ps1 tools/release-packages.json docs/dev/release-runbook.md tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs _bmad-output/implementation-artifacts/deferred-work.md`.

### Review Findings

- [x] [Review][Patch] Absolute inventory/schema override paths are resolved under the repo root [tools/validate-release-packages.ps1:12].
- [x] [Review][Patch] Malformed direct `-Version` inputs can normalize to an empty or invalid comparison version [tools/validate-release-packages.ps1:120].
- [x] [Review][Patch] `IsPackable` validation reads the first XML property and can miss later or conditional values [tools/validate-release-packages.ps1:52].
- [x] [Review][Patch] Release-package Python fixtures can delete pre-existing sentinel project content [tests/tooling/release_packages/release_packages_test.py:234].
- [x] [Review][Patch] Task 7 and completion notes overclaim validator and fixture execution [14-2-release-pipeline-audit-hardening.md:62].
- [x] [Review][Patch] Release runbook mislabels the `github-actions[bot]` user id as the GitHub Actions App ID [docs/dev/release-runbook.md:145].

## File Scope

Allowed files for this story:

- `.github/workflows/release.yml` - UPDATE. Release workflow action pinning, stale-tag preflight or documented deferral, and partial-publish signal wiring.
- `.releaserc.json` - UPDATE only if stale-tag handling or semantic-release configuration changes.
- `tools/validate-release-packages.ps1` - UPDATE. Complete package/non-package inventory validation, schema validation, and build-metadata behavior.
- `tools/release-packages.json` - UPDATE only to add a schema reference or inventory corrections.
- `tools/release-packages.schema.json` - NEW optional. Preferred home for inventory schema if the implementation chooses a file-backed schema.
- `tools/pack-release.ps1` - UPDATE only if needed to keep version normalization/rejection consistent with package validation.
- `tools/publish-nuget.ps1` - UPDATE only if partial-publish signal audit finds a concrete gap in the existing summary/annotation behavior.
- `tools/create-partial-publish-issue.ps1` - UPDATE only if partial-publish issue creation/commenting needs hardening.
- `tests/tooling/publish_nuget/publish_nuget_test.py` - UPDATE for partial-publish issue or summary behavior.
- `tests/tooling/release_packages/**` - NEW optional. Use for focused package inventory/schema/version validation tests if Python fixtures are the lowest-risk path. Recursive glob so the story-file-scope check matches files inside the new directory.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` - UPDATE. Narrow release-lane workflow/script assertions.
- `docs/dev/release-runbook.md` - UPDATE. Checksum/equivalent package evidence and release bot identity guidance.
- `_bmad-output/implementation-artifacts/deferred-work.md` - UPDATE. Resolve or carry forward targeted deferred IDs with evidence.
- `_bmad-output/implementation-artifacts/14-2-release-pipeline-audit-hardening.md` - UPDATE. Record implementation notes, validation, review findings, and file list.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad workflow/status transitions.

Read/verify only:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-03.md`
- `_bmad-output/implementation-artifacts/11-2-semantic-release-and-nuget-publishing.md`
- `_bmad-output/implementation-artifacts/12-1-first-release-path-validation.md`
- `_bmad-output/implementation-artifacts/12-5-partial-publish-alerting.md`
- `_bmad-output/implementation-artifacts/14-1-ci-story-scope-enforcement-hardening.md`
- `package.json`
- `package-lock.json`
- `src/**/*.csproj`

Forbidden by default:

- `src/**/*.cs`
- `tests/**/*.cs` except `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `tools/test-release.ps1` unless only a release-validation test proves the script invocation contract must be corrected
- `Directory.Packages.props`
- `Directory.Build.props`
- `NuGet.config`
- `package-lock.json`
- `Hexalith.AI.Tools/**`
- `Hexalith.Commons/**`
- `Hexalith.EventStore/**`

## Dev Notes

### Current Implementation State

The release path is already script-driven and should stay that way. `.github/workflows/release.yml` restores, builds, runs `tools/test-release.ps1`, installs npm tooling, runs `tools/validate-release-packages.ps1`, then delegates packing and publishing to `npx semantic-release`. `.releaserc.json` uses `@semantic-release/exec` to call `tools/pack-release.ps1` as `prepareCmd` and `tools/publish-nuget.ps1` as `publishCmd`, then `@semantic-release/github` uploads `artifacts/packages/release/*.nupkg`.

Story 12.5 already added structured partial-publish summaries and a release-workflow issue alert. Treat that as existing behavior to verify, not as a blank slate to rebuild. The current flow writes `artifacts/packages/release/publish-summary.json` on publish failure, emits a `PARTIAL PUBLISH - manual reconciliation required` annotation when at least one package pushed and one failed or was not attempted, and calls `tools/create-partial-publish-issue.ps1` from the failed workflow. If the audit finds that this fully satisfies `S11-FD`, close the deferred entry with test/runbook evidence.

`tools/release-packages.json` currently lists seven packable packages and three non-packable projects. `src/` currently contains ten `.csproj` files, so a complete inventory check should account for all ten exactly once:

- Packable: `Hexalith.Memories.Contracts`, `Hexalith.Memories.Client.Rest`, `Hexalith.Memories.Redis`, `Hexalith.Memories.Cli`, `Hexalith.Memories.Mcp`, `Hexalith.Memories.EventStore`, `Hexalith.Memories.Telemetry`.
- Non-packable: `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj`, `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj`, `src/Hexalith.Memories.ServiceDefaults/Hexalith.Memories.ServiceDefaults.csproj`.

The current validator checks that listed non-packable projects exist and have `<IsPackable>false</IsPackable>`, but it does not yet fail when a new non-packable `src/**/*.csproj` is omitted from the inventory. That is the core `W2` gap.

### Deferred IDs Targeted

This story is the normal lifecycle home for:

- `W1`: SHA-pin or explicitly defer release workflow action references.
- `W2`: complete non-packable project inventory enforcement.
- `W12`: schema reference or schema validation for `tools/release-packages.json`.
- `W15`: direct `validate-release-packages.ps1 -Version 1.0.0+local` build-metadata behavior.
- `W16`: replace `Where-Object { $_.Name -notlike "*.snupkg" }` with clearer `.nupkg` extension filtering if touched in validation/publish paths.
- `W19`: release concurrency/stuck-release handling, likely resolved by the current partial-publish alerting model or carried forward with a focused defer-by.
- `S11-FC`: stale tag collision handling.
- `S11-FD`: partial-publish alerting on the release pipeline.
- `12.1-RV1`: checksum or equivalent package evidence in the release runbook.
- `12.1-RV2`: release bot identity evidence in the release runbook.

Do not sweep unrelated deferred entries into this story. `12.1-RV3` skip-CI edge case, `12.1-RV4` package-lock verification, and `12.1-RV5` CONTRIBUTING cross-link are adjacent but not part of the five ACs unless the implementation naturally touches those exact areas and records why.

### Implementation Guardrails

- Do not add new release infrastructure unless it closes a named acceptance criterion. Prefer tightening the existing PowerShell scripts and tests.
- Do not replace semantic-release. The release contract already depends on `.releaserc.json`, `@semantic-release/exec`, `@semantic-release/github`, `tools/pack-release.ps1`, and `tools/publish-nuget.ps1`.
- Do not broaden package validation into runtime source changes. The package inventory source of truth is project metadata plus `tools/release-packages.json`.
- Do not initialize or update nested submodules. Root-level submodule pointer changes are forbidden by default for this story.
- Avoid new Python, PowerShell, or npm dependencies unless there is no standard-library or built-in PowerShell/.NET option.
- Keep all diagnostics safe for public CI logs. Never print `NUGET_API_KEY`, package API tokens, or raw secret-bearing command lines.
- If action SHA pinning is implemented, pin only immutable action references. Do not SHA-pin local `run:` steps; they are repository code.

### Technical Constraints and References

- GitHub's Actions hardening guidance recommends pinning actions to a full-length commit SHA for stronger supply-chain control. Source: https://docs.github.com/actions/learn-github-actions/security-hardening-for-github-actions
- The release job currently uses `GITHUB_TOKEN` for semantic-release GitHub API operations and `NUGET_API_KEY` for NuGet publishing. Semantic-release documents GitHub Actions usage and CI authentication expectations. Sources: https://semantic-release.gitbook.io/semantic-release/recipes/ci-configurations/github-actions and https://semantic-release.gitbook.io/semantic-release/usage/ci-configuration
- `dotnet nuget verify` and `nuget verify -Signatures` can provide package signature verification evidence, but they are not checksum substitutes for every package/source scenario. If used, record exactly which command and result the runbook expects. Sources: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-verify and https://learn.microsoft.com/en-us/nuget/reference/cli-reference/cli-ref-verify
- PowerShell `Test-Json` supports schema validation with `-SchemaFile` in current PowerShell versions. If used, confirm the repository's release runner PowerShell version supports the selected mode, or fall back to explicit object-property validation inside `validate-release-packages.ps1`. Source: https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/test-json

### Testing Requirements

Minimum validation before review:

```powershell
pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1
python -m unittest discover -s tests/tooling/publish_nuget -p "*_test.py"
dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~CiTestInventoryTests"
git diff --check -- .github/workflows/release.yml .releaserc.json tools/validate-release-packages.ps1 tools/release-packages.json docs/dev/release-runbook.md tests/tooling/publish_nuget/publish_nuget_test.py tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs _bmad-output/implementation-artifacts/deferred-work.md
```

Additional probes to record when relevant:

- `tools/release-packages.json` with a misspelled package field fails before pack/publish.
- A new non-packable project under `src/` that is not listed in `nonPackableProjects` fails validation.
- A direct `-Version` value containing build metadata either fails or normalizes with the selected message.
- Partial-publish summary/issue behavior remains deterministic and does not leak `NUGET_API_KEY`.
- Release runbook evidence commands can be copied by an operator without manually publishing packages.

## Project Structure Notes

- This is a release tooling and governance story. Expected implementation is limited to `.github/workflows`, `tools`, `tests/tooling`, one focused C# CI inventory test file, release docs, and BMAD artifacts.
- The repository already uses PowerShell for release validation and Python stdlib `unittest` for publish-tooling fixtures. Mirror those patterns instead of adding a new test framework.
- The `Hexalith.Commons` `project-context.md` discovered by the persistent-facts glob is background convention only because it belongs to a submodule/sibling repository. Story-local file scope and repository-specific artifacts take precedence.

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 14 and Story 14.2 acceptance criteria.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-03.md` - approved Epic 14 scope and story ordering.
- `_bmad-output/implementation-artifacts/14-1-ci-story-scope-enforcement-hardening.md` - previous story context and Epic 14 guardrails.
- `_bmad-output/implementation-artifacts/11-2-semantic-release-and-nuget-publishing.md` - original release automation story context.
- `_bmad-output/implementation-artifacts/12-1-first-release-path-validation.md` - release runbook and branch-protection evidence source.
- `_bmad-output/implementation-artifacts/12-5-partial-publish-alerting.md` - partial-publish alerting implementation context.
- `_bmad-output/implementation-artifacts/deferred-work.md` - target deferred IDs and closure bookkeeping.
- `.github/workflows/release.yml` - current release workflow.
- `.releaserc.json` - semantic-release plugin chain and tag format.
- `tools/validate-release-packages.ps1` - canonical package inventory and package metadata validator.
- `tools/release-packages.json` - approved package inventory.
- `tools/pack-release.ps1` - semantic-release prepare command.
- `tools/publish-nuget.ps1` - NuGet publish command and partial-publish summary writer.
- `tools/create-partial-publish-issue.ps1` - partial-publish GitHub issue/comment helper.
- `docs/dev/release-runbook.md` - operator release evidence and recovery guidance.
- `tests/tooling/publish_nuget/publish_nuget_test.py` - existing Python fixture pattern for release publish tooling.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` - existing CI/release-lane inventory guard tests.
- GitHub Actions hardening docs: https://docs.github.com/actions/learn-github-actions/security-hardening-for-github-actions
- Semantic-release GitHub Actions docs: https://semantic-release.gitbook.io/semantic-release/recipes/ci-configurations/github-actions
- Semantic-release CI configuration docs: https://semantic-release.gitbook.io/semantic-release/usage/ci-configuration
- .NET `dotnet nuget verify` docs: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-verify
- NuGet CLI `verify` docs: https://learn.microsoft.com/en-us/nuget/reference/cli-reference/cli-ref-verify
- PowerShell `Test-Json` docs: https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/test-json

## Dev Agent Record

### Agent Model Used

GPT-5
Claude Opus 4.7 (1M context) — implementation pass on 2026-05-04.

### Debug Log References

- Pre-dev hardening preflight passed on 2026-05-03T11:16:13Z with all checks green and `0 dirty paths`.
- Story selection chose `14-2-release-pipeline-audit-hardening` because `ready_count` was below the target of `5` and this was the first backlog story in sprint-status order.
- `/bmad-create-story 14-2-release-pipeline-audit-hardening` context gathering loaded Epic 14 planning, the approved 2026-05-03 sprint-change proposal, Story 14.1, current release workflow, semantic-release config, package validation/publish scripts, release runbook, publish-tooling tests, deferred-work entries, recent git history, and current official GitHub, semantic-release, NuGet, and PowerShell documentation.
- 2026-05-04 implementation pass resolved `actions/*` reference SHAs via `gh api repos/<owner>/<repo>/commits/<tag>` for all five third-party actions in `release.yml` (checkout v6 → de0fac2, setup-dotnet v5 → c2fa09f, setup-node v4 → 49933ea, cache v5 → 27d5ce7, upload-artifact v5 → 330a01c).
- 2026-05-04 implementation pass: `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Release --no-build` returned 342/342 (full Cli.Tests suite green; up from the Story 14.1 close-out baseline of 333 by the seven new release-workflow tests added here plus the structural fixtures that already exist).
- 2026-05-04 implementation pass: focused `--filter "FullyQualifiedName~CiTestInventoryTests"` returned 19/19 including all seven new `ReleaseWorkflow_*` assertions and the unchanged `TestReleaseBaselineFilters_ShouldMatchOpenDeferredWorkEntries` guardrail.
- 2026-05-04 implementation pass: `git diff --check` against the touched paths emitted only LF/CRLF normalization warnings (Windows working tree), no whitespace errors.
- 2026-05-04 code-review close-out patched 6/6 findings: absolute inventory/schema overrides now respect rooted paths, malformed direct `-Version` inputs fail before normalization, multiple or conditional `<IsPackable>` declarations fail loudly, release-package sentinel fixtures refuse to reuse existing `src/` directories, the release workflow invokes the Python release-package fixture suite before semantic-release, and the runbook now distinguishes the GitHub Actions app URL from the `github-actions[bot]` user id.
- 2026-05-04 final local validation used repo-local tools installed under `.tools/` for this review run only: PowerShell 7.6.1 portable and .NET SDK 10.0.201. Validation passed: `tools/validate-release-packages.ps1` before fixtures, `python -m unittest discover -s tests/tooling/release_packages -p "*_test.py"` 18/18, `tools/validate-release-packages.ps1` again after fixtures to confirm no temporary sentinel projects remained, focused `CiTestInventoryTests` 19/19, and `git diff --check` clean aside from LF/CRLF normalization warnings.

### Completion Notes List

- Story context created on 2026-05-03.
- The story deliberately keeps scope on release workflow, package validation, release evidence, and deferred-work closure. It does not reopen runtime, OIDC, embedding, migration, package-lock, or submodule work.
- The implementation guidance treats Story 12.5 partial-publish alerting as existing behavior to audit and close out, not as a feature to reinvent.
- Target deferred IDs are called out so implementation can resolve bookkeeping without losing audit evidence.
- AC1 — release workflow hardening. All five third-party `actions/*` references in `.github/workflows/release.yml` are SHA-pinned to 40-char commit SHAs with trailing `# v<x.y.z>` comments; the `concurrency` block keeps `cancel-in-progress: false` deliberately so an interrupted publish cannot convert into an indeterminate half-state, with an inline rationale comment naming the trade-off; the `permissions` block stays at `contents: write`, `issues: write`, `pull-requests: write` — no broader scopes added; partial-publish signaling (existing structured `publish-summary.json` + `PARTIAL PUBLISH` annotation + step summary + `tools/create-partial-publish-issue.ps1` issue/comment helper) was audited and confirmed sufficient; `S11-FC` stale-tag preflight stays carried forward with a fresh defer-by 2026-08-04 because neither the `npx semantic-release --dry-run` cost nor a custom version-computation path meets the cost/benefit bar without an observed collision; `S11-FD` is closed with test/runbook evidence.
- AC2 + AC4 — package inventory validation and schema. New `tools/release-packages.schema.json` enforces required keys, `additionalProperties: false`, and `pattern` constraints; `tools/release-packages.json` references the schema via `$schema`; `tools/validate-release-packages.ps1` runs `Test-Json -SchemaFile` before any structural use, iterates every `src/**/*.csproj`, requires explicit unconditional `<IsPackable>true|false</IsPackable>` declarations, asserts every project appears in exactly one of `packages` or `nonPackableProjects`, respects rooted inventory/schema override paths, and rejects duplicate package IDs, duplicate project paths, cross-bucket entries, multiple `<IsPackable>` declarations, and conditional `<IsPackable>` declarations with named diagnostics. NuGet package-id casing remains case-sensitive (`-cne` comparison preserved). Python fixture suite at `tests/tooling/release_packages/release_packages_test.py` covers misspelled top-level fields (`nonPackableProject`), misspelled inner fields (`packageID`, `projectPath`), duplicate package IDs (different paths), duplicate project paths (different IDs), cross-bucket clashes, missing non-packable projects, extra non-packable / packable projects pointing at non-existent csproj, the five IsPackable failure modes (missing, blank, unsupported, duplicate, conditional), and invalid `-Version` inputs.
- AC2 — build-metadata normalization. `ConvertTo-NormalizedNuGetVersion` validates direct `-Version` inputs before stripping `+...`, emits a `Note:` diagnostic naming both the original and NuGet-normalized form for valid build metadata, and threads the normalized version through both per-package and internal cross-package dependency-version assertions. `tools/pack-release.ps1` is unchanged because the semantic-release-driven CI path always passes valid semantic-release versions; the normalization is a direct-operator UX fix that keeps semantic-release versions and NuGet package versions in lockstep while rejecting malformed direct input.
- AC3 — release evidence. `docs/dev/release-runbook.md` adds two subsections: `Release Identity And Forensic Anchors` pinning the GitHub Actions app URL (`https://github.com/apps/github-actions`) and the `github-actions[bot]` user id (`41898282`) as the canonical token identity evidence for tag, GitHub Release, and package-asset writes plus the four anchors reviewers must capture per release; and `Per-Release Package Audit Evidence` requiring SHA-256 capture for every future release with deterministic Windows pwsh (`Get-FileHash -Algorithm SHA256`) and Linux (`sha256sum`) commands, plus `dotnet nuget verify --all` and `nuget verify -Signatures` as audit-equivalent signature-based options. The historical `v1.2.0` block remains as-is because the source CI artifacts are no longer locally available; the requirement applies to releases after Story 14.2.
- AC5 — release-lane test assertions. `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` adds eight anchored assertions: `ReleaseWorkflow_ValidatePackageInventoryStep_RunsCanonicalScript`, `ReleaseWorkflow_ReleasePackageValidatorFixtures_RunBeforePublish`, `ReleaseWorkflow_RunSemanticReleaseStep_UsesCanonicalCommand`, `ReleaseWorkflow_AlertPartialPublishStep_GuardsOnFailureAndReferencesAuditPaths`, `ReleaseWorkflow_TestReleaseStep_DelegatesToSharedScript`, `ReleaseWorkflow_ThirdPartyActions_ArePinnedToCommitSha`, `ReleaseWorkflow_TopLevelPermissions_AreLeastPrivilege`, and `ReleaseWorkflow_Concurrency_PreservesPartialPublishSelfHeal`. Helpers `ParseReleaseWorkflowSteps` and `GetReleaseWorkflowTopLevelMapping` parse the workflow by 6-space step header / 8-space body indent and 2-space mapping indent; the local hand-rolled parser anchors every check to a step name or top-level mapping key instead of raw file-wide `Contains` (W23 spirit, not a full closure). `TestReleaseBaselineFilters_ShouldMatchOpenDeferredWorkEntries` and the rest of the existing release-baseline parsing tests are preserved.
- AC1-5 — deferred-work bookkeeping. Closed with evidence: `W1`, `W2`, `W12`, `W15`, `W19`, `S11-FD`, `12.1-RV1`, `12.1-RV2`. Partially closed: `W16` (validate-release-packages.ps1 cleaned; publish-nuget.ps1 mirror left as-is per file scope's "concrete partial-publish gap" gate). Carried forward: `S11-FC` (stale-tag preflight) with fresh defer-by 2026-08-04. The Story 11.1+11.2 entries `W3..W11`, `W13`, `W14`, `W17`, `W18`, `W20..W24`, `S11-FB` and the Story 12.1 entries `12.1-RV3`, `12.1-RV4`, `12.1-RV5` are explicitly out of 14.2 scope and remain open with their original triggers.
- File scope discipline. `tools/pack-release.ps1`, `tools/publish-nuget.ps1`, `tools/create-partial-publish-issue.ps1`, `tests/tooling/publish_nuget/publish_nuget_test.py`, and `.releaserc.json` are intentionally NOT modified because no concrete gap was found in the audit: existing partial-publish behavior is correct, semantic-release plugins remain stable, and `pack-release.ps1` is compatible with the `-Version` normalization. `Hexalith.Memories.sln` (untracked) is not in the file scope and is unmodified.

### File List

- `.github/workflows/release.yml` — UPDATED. SHA-pinned all third-party action references; added inline rationale comments for `concurrency: cancel-in-progress: false` and the least-privilege `permissions` block; added `Run release package validator fixtures` before semantic-release.
- `tools/release-packages.schema.json` — NEW. JSON Schema draft-07 for the release package inventory with `additionalProperties: false`, `pattern` constraints on package IDs and project paths, and `uniqueItems`.
- `tools/release-packages.json` — UPDATED. Added `$schema` reference to the new schema; package set unchanged.
- `tools/validate-release-packages.ps1` — UPDATED. Added schema validation entry point, full `src/**/*.csproj` iteration with explicit unconditional `IsPackable` declaration check, rooted inventory/schema override resolution, duplicate-ID and duplicate-project guards, cross-bucket guard, malformed direct-version rejection before build-metadata normalization, and replaced `Where-Object {-notlike "*.snupkg"}` with `Where-Object { $_.Extension -ieq '.nupkg' }` (W16 partial closure).
- `tests/tooling/release_packages/release_packages_test.py` — NEW. Python `unittest` fixture suite covering schema rejection of misspelled fields, duplicate ID / project / cross-bucket guards, missing / extra / non-existent inventory entries, missing / blank / unsupported / duplicate / conditional `<IsPackable>` values, invalid direct-version inputs, and `-Version 1.2.3+local` normalization with the named diagnostic. Review close-out made sentinel project cleanup refuse pre-existing `src/` directories before creating temporary projects.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` — UPDATED. Added eight `ReleaseWorkflow_*` assertions plus the local hand-rolled `ParseReleaseWorkflowSteps` and `GetReleaseWorkflowTopLevelMapping` helpers and the `ReleaseWorkflowStep` record. `TestReleaseBaselineFilters_ShouldMatchOpenDeferredWorkEntries` preserved verbatim.
- `docs/dev/release-runbook.md` — UPDATED. Added `Release Identity And Forensic Anchors` (pins the GitHub Actions app URL and `github-actions[bot]` user id `41898282`, lists four per-release forensic anchors) and `Per-Release Package Audit Evidence` (SHA-256 commands for Windows pwsh and Linux, `dotnet nuget verify --all` and `nuget verify -Signatures` as audit-equivalents) subsections.
- `_bmad-output/implementation-artifacts/deferred-work.md` — UPDATED. Inline `[resolved in 14.2]` markers on `W1`, `W2`, `W12`, `W15`, `W16` (partial), `W19`, `S11-FD`, `12.1-RV1`, `12.1-RV2`; refreshed `S11-FC` with concrete trigger and defer-by 2026-08-04; new `## Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)` rollup section.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — UPDATED. `14-2-release-pipeline-audit-hardening` in-progress → done after review patch validation; `last_updated` timestamp refreshed.
- `_bmad-output/implementation-artifacts/14-2-release-pipeline-audit-hardening.md` — UPDATED. Review findings recorded and checked off, Task 7 validation completed, Status set to `done`, Dev Agent Record + File List + Change Log updated.

### Change Log

- 2026-05-03: Created Story 14.2 and promoted it from `backlog` to `ready-for-dev`.
- 2026-05-04: Implementation pass. SHA-pinned `release.yml` third-party actions; added `release-packages.schema.json` plus `Test-Json` validation, full `src/` inventory enforcement, `IsPackable` discipline, duplicate / cross-bucket guards, and `-Version` build-metadata normalization in `validate-release-packages.ps1`; added Python fixture suite for the new validator behavior; added seven anchored `ReleaseWorkflow_*` C# assertions; added `Release Identity And Forensic Anchors` and `Per-Release Package Audit Evidence` runbook subsections; closed `W1`, `W2`, `W12`, `W15`, `W19`, `S11-FD`, `12.1-RV1`, `12.1-RV2` with evidence; partially closed `W16`; carried `S11-FC` forward with fresh defer-by 2026-08-04. Status: `ready-for-dev` → `in-progress` → `review`.
- 2026-05-04: Code-review close-out. Applied 6/6 review patches: rooted inventory/schema path handling, malformed direct-version rejection, duplicate/conditional `IsPackable` rejection, safer sentinel fixture cleanup, release workflow Python fixture execution, corrected GitHub Actions bot identity wording, and honest Task 7 validation bookkeeping. Installed repo-local validation tools, reran all required validation green, and moved status `in-progress` → `done`.

## Story Completion Status

Story complete on 2026-05-04. All review findings are patched and checked off. Final validation is green: release package validator passed before and after fixture execution, Python release-package fixtures 18/18 passed, focused `CiTestInventoryTests` 19/19 passed under .NET SDK 10.0.201, and `git diff --check` passed. Status set to `done`.
