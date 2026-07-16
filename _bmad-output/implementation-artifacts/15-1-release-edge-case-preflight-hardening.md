# Story 15.1: Release Edge-Case Preflight Hardening

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a release maintainer,
I want stale-tag and skip-CI edge cases handled before release execution,
so that releases do not fail late, silently skip, or leave ambiguous audit evidence.

## Acceptance Criteria

1. Given stale release tags can collide with `.releaserc.json` `tagFormat: "v${version}"`, when release preflight behavior is reassessed, then deferred ID `S11-FC` is resolved with a concrete preflight, accepted with refreshed rationale, or carried forward with a new defer-by date and trigger.

2. Given release workflow skip logic reads commit messages, when a PR merge or squash body contains `[skip ci]` as quoted text, then deferred ID `12.1-RV3` is resolved by documentation, tests, or workflow guardrails, or explicitly accepted with rationale.

3. Given release tooling depends on Node package restore, when release package validation is reviewed, then deferred ID `12.1-RV4` is resolved by confirming `package-lock.json` tracking and fresh-clone behavior, or carried forward with a concrete owner and trigger.

4. Given release-hardening decisions change workflow, runbook, or tooling behavior, when the story completes, then focused validation covers the changed behavior and `deferred-work.md` records `resolved`, `accepted`, or `carried-forward` evidence for every targeted ID.

## Tasks / Subtasks

- [x] Task 1 - Reassess stale-tag collision handling (AC: 1, 4)
  - [x] Read `.releaserc.json`, `package.json`, `.github/workflows/release.yml`, `docs/dev/release-runbook.md`, and the current `S11-FC` entry in `deferred-work.md` before changing anything.
  - [x] Decide whether stale-tag protection belongs in a new preflight script, an inline workflow step before `npx semantic-release`, a release-runbook acceptance decision, or a refreshed carried-forward entry.
  - [x] If implementing a preflight, compare the tag that semantic-release would create against remote tags without running prepare/publish side effects. Do not invent custom version calculation unless tests prove it matches the semantic-release commit analyzer for this repository.
  - [x] If accepting or carrying forward the risk, record why the cost of `semantic-release --dry-run` or a custom preflight still does not justify a workflow change and set a fresh trigger/defer-by date.

- [x] Task 2 - Guard or document skip-CI release bypass behavior (AC: 2, 4)
  - [x] Review the current release workflow job-level condition: `!contains(github.event.head_commit.message, '[skip ci]') && !contains(github.event.head_commit.message, '[skip actions]')`.
  - [x] Decide whether the release job should keep this condition, narrow it, replace it with a repository-owned skip predicate, or document an accepted risk for merge/squash messages that quote skip tokens.
  - [x] Add focused coverage in `CiTestInventoryTests` if workflow behavior changes or if a parser/guard contract is added.
  - [x] Update `docs/dev/release-runbook.md` or `CONTRIBUTING.md` so maintainers know whether `[skip ci]` may appear in PR titles/bodies, squash messages, revert text, or quoted examples.

- [x] Task 3 - Confirm package-lock and fresh-clone release restore behavior (AC: 3, 4)
  - [x] Verify `package-lock.json` is tracked, matches the root `package.json`, and is used by the release workflow through `npm ci`.
  - [x] Run or script a fresh-clone-style package restore check that does not rely on an existing `node_modules` directory. A local `npm ci --ignore-scripts` or a temporary copy is acceptable if it proves lock/package consistency without mutating tracked files.
  - [x] If the current workflow already satisfies `12.1-RV4`, close it with evidence. If not, add the smallest workflow/tooling/doc change that makes the failure mode loud before semantic-release.
  - [x] Do not update package dependencies or rewrite `package-lock.json` unless the check proves it is stale and the story explicitly records why.

- [x] Task 4 - Update deferred-work dispositions (AC: 1-4)
  - [x] Add a Story 15.1 rollup section to `_bmad-output/implementation-artifacts/deferred-work.md` or update the existing structured entries for `S11-FC`, `12.1-RV3`, and `12.1-RV4`.
  - [x] Use the Story 14.5 schema fields exactly: `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, and either `Evidence` or `Rationale`.
  - [x] Do not remove historical context. Mark each targeted ID `resolved`, `accepted`, or `carried-forward` with concrete evidence or rationale.
  - [x] Do not sweep `12.1-RV5` or unrelated release/governance entries unless a touched file naturally closes them and the story records the reason.

- [x] Task 5 - Validate release-edge changes (AC: 1-4)
  - [x] Run `pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1`.
  - [x] Run `npm ci --ignore-scripts` or the selected fresh-clone equivalent and record the result.
  - [x] If `.github/workflows/release.yml` or `CiTestInventoryTests.cs` changes, run `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~CiTestInventoryTests"`.
  - [x] If a new release preflight script is added, run its focused positive and negative tests, including a stale-tag collision fixture and a no-release/no-next-version fixture when relevant.
  - [x] Run `git diff --check -- .github/workflows/release.yml .releaserc.json package.json package-lock.json tools docs/dev/release-runbook.md CONTRIBUTING.md tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/15-1-release-edge-case-preflight-hardening.md`.

## File Scope

Allowed files for this story:

- `.github/workflows/release.yml` - UPDATE only if release skip or stale-tag preflight behavior changes.
- `.releaserc.json` - UPDATE only if semantic-release configuration must change to make stale-tag handling explicit.
- `package.json` - UPDATE only if adding a repository-owned release preflight npm script is the chosen implementation path.
- `package-lock.json` - UPDATE only if `npm ci` or lock/package consistency proves it is stale; otherwise read/verify only.
- `tools/release-preflight.ps1` - NEW optional. Preferred if a small PowerShell preflight gives clearer stale-tag or skip-token validation than embedding logic in YAML.
- `tests/tooling/release_preflight/**` - NEW optional. Use if a new script needs fixture coverage.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` - UPDATE for workflow-contract assertions when release workflow behavior changes.
- `docs/dev/release-runbook.md` - UPDATE. Document final release-edge behavior and operator evidence expectations.
- `CONTRIBUTING.md` - UPDATE only if contributor guidance for skip tokens or release PR messages is needed.
- `_bmad-output/implementation-artifacts/deferred-work.md` - UPDATE. Record `S11-FC`, `12.1-RV3`, and `12.1-RV4` disposition.
- `_bmad-output/implementation-artifacts/15-1-release-edge-case-preflight-hardening.md` - UPDATE. Record implementation notes, validation, review findings, and file list.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad workflow/status transitions.

Read/verify only:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/12-1-first-release-path-validation.md`
- `_bmad-output/implementation-artifacts/14-2-release-pipeline-audit-hardening.md`
- `_bmad-output/implementation-artifacts/14-5-deferred-register-governance-and-sprint-status-hygiene.md`
- `.github/workflows/ci.yml`
- `tools/validate-release-packages.ps1`
- `tools/release-packages.json`
- `tools/release-packages.schema.json`
- `tools/pack-release.ps1`
- `tools/publish-nuget.ps1`
- `tools/create-partial-publish-issue.ps1`

Forbidden by default:

- `src/**`
- `tests/**/*.cs` except `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `Directory.Packages.props`
- `Directory.Build.props`
- `NuGet.config`
- `Hexalith.AI.Tools/**`
- `Hexalith.Commons/**`
- `Hexalith.EventStore/**`
- Any submodule pointer change

## Dev Notes

### Current Implementation State

The release workflow runs on `push` to `main`, skips the job when the head commit message contains `[skip ci]` or `[skip actions]`, restores .NET, builds, runs `tools/test-release.ps1`, runs `npm ci`, validates package inventory, runs the release-package Python fixtures, then runs `npx semantic-release`.

`.releaserc.json` currently uses `tagFormat: "v${version}"` and the plugin chain `@semantic-release/commit-analyzer`, `@semantic-release/release-notes-generator`, `@semantic-release/exec`, and `@semantic-release/github`. It no longer uses `@semantic-release/git`, so the current intended release path creates the tag and GitHub Release without committing generated files back to `main`.

Story 14.2 already hardened the release lane by SHA-pinning third-party actions, enforcing package inventory/schema checks, adding release-package fixtures, validating release workflow steps from `CiTestInventoryTests`, and documenting release identity/package audit evidence. Treat those controls as existing infrastructure to preserve.

`package-lock.json` is tracked in this repository and the root `package.json` exposes `release:dry-run` as `semantic-release --dry-run --no-ci`. `npm ci` is already the release workflow restore command. Story 15.1 should prove the lock/package contract is still true and close or carry forward `12.1-RV4` with evidence instead of merely noting the file exists.

### Deferred IDs Targeted

This story owns exactly these IDs unless implementation naturally touches an adjacent item and records why:

- `S11-FC`: stale tag collision handling for semantic-release `tagFormat: "v${version}"`. Story 14.2 carried this forward with defer-by `2026-08-04` because a dry-run or custom version-computation preflight was not justified without an observed collision.
- `12.1-RV3`: PR merge/squash commit message containing `[skip ci]` as quoted text could skip the release workflow because the job condition reads `github.event.head_commit.message`.
- `12.1-RV4`: verify the tracked `package-lock.json` and fresh-clone `npm ci` behavior for release tooling.

Do not fold `12.1-RV5` into this story by default. It is a CONTRIBUTING cross-link/discoverability item, not a release-edge preflight requirement unless the chosen skip-token guidance naturally belongs there.

### Implementation Guardrails

- Do not replace semantic-release or duplicate its release calculation casually. If a preflight computes the next version, it must be tested against the same commit-analyzer assumptions used by this repository.
- Prefer fail-loud preflight behavior over late semantic-release failure, but keep release runtime cost reasonable. A full `semantic-release --dry-run` before the actual release may be acceptable only if the story records why the double analysis cost and push-permission check are worth it.
- Keep skip-token behavior explicit. GitHub's native skip instructions apply to `push` and `pull_request` workflows, but this repository also has an explicit job-level condition. A maintainer should be able to predict whether a release will run from the merged commit message alone.
- Do not rely on broad file-wide `Contains` assertions for workflow behavior if touching `CiTestInventoryTests`; use existing step/top-level YAML helper patterns from Story 14.2 where possible.
- Release diagnostics must not print `NUGET_API_KEY`, `GITHUB_TOKEN`, npm tokens, or full environment dumps.
- Do not initialize or update nested submodules. Do not change root-level submodule pointers.

### Party-Mode Review Clarifications - 2026-05-12

- Treat S11-FC as a release-owner decision, not a passive inspection task. If implemented, stale-tag protection must fail before `npx semantic-release` starts publish-capable work, name the exact conflicting `refs/tags/v<version>` ref, and avoid substring matches such as `v1.2.3` matching `v1.2.30` or `v1.2.3-rc.1`. If accepted or carried forward instead, record the release maintainer owner, rationale, fresh trigger, and defer-by date.
- If a release preflight script is added, keep it release-only, independently callable outside GitHub Actions, and isolated from runtime source, package publishing internals, and submodule state. Tests must use temporary repositories or local bare remotes for tag fixtures and must never mutate real project tags or remotes.
- Stale-tag test coverage should distinguish no tag, local-only tag, remote-only tag, and matching local+remote tag cases when the implementation path performs tag checks. Exact ref checks are required; broad text searches over tag output are not sufficient evidence.
- Resolve the skip-CI edge by making the release workflow outcome predictable from the merged commit message. The story must explicitly document whether the workflow ignores quoted skip tokens, rejects them before release execution, removes the job-level `github.event.head_commit.message` gate, or intentionally accepts the risk.
- Skip-token coverage should include unquoted `[skip ci]`, double-quoted `"[skip ci]"`, single-quoted `'[skip ci]'`, escaped quotes, multiline merge/squash commit bodies, body-only tokens, and unrelated `skip ci` text without brackets. Record whether subject-only or full-message inspection is the intended contract.
- Fresh-clone package-lock proof must be reproducible and auditable. At minimum, record that `package-lock.json` is tracked, the command and working directory used for `npm ci --ignore-scripts` or an isolated worktree/fresh-clone equivalent, the assumption that no existing `node_modules` state is reused, and whether tracked files stayed clean afterward.
- Deferred-work updates for `S11-FC`, `12.1-RV3`, and `12.1-RV4` must use the exact Story 14.5 field labels: `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, and `Evidence` for `resolved` or `Rationale` for `accepted` / `carried-forward`. The three statuses are mutually exclusive; `accepted` means intentional risk acceptance with owner, trigger, and defer-by date.
- Operator-facing failure messages must be actionable. Any stale-tag, skip-token, or npm-restore failure should state what blocked the release, which evidence was checked, and what the release maintainer should do next without exposing tokens or full environment dumps.
- Keep `12.1-RV5` and broad CONTRIBUTING policy work out of scope unless the same release-edge change naturally closes it; if touched, record why that adjacent closure was unavoidable.
- Before review, record a file-scope check against the allowed file list. No `src/**`, unrelated `tests/**`, package/project metadata outside the story scope, submodule content, or submodule pointer changes are allowed.

### Technical Constraints and References

- GitHub Actions recognizes skip instructions such as `[skip ci]`, `[ci skip]`, `[no ci]`, `[skip actions]`, and `[actions skip]` in commit messages for `push` and `pull_request` workflow triggers. GitHub also warns that skipped required checks can remain pending and that skip instructions apply only to `push` and `pull_request` events. Source: https://docs.github.com/en/actions/how-tos/manage-workflow-runs/skip-workflow-runs
- semantic-release uses `tagFormat` to identify release tags, defaults to `v${version}`, and requires the `version` variable exactly once. Its dry-run mode skips prepare/publish/addChannel/success/fail while printing the next version and release notes, but still verifies repository push permission. Source: https://semantic-release.gitbook.io/semantic-release/usage/configuration
- semantic-release documents `reference already exists` failures when the tag it is about to create already exists outside the release branch history. Source: https://semantic-release.gitbook.io/semantic-release/support/troubleshooting
- `npm ci` requires an existing `package-lock.json` or `npm-shrinkwrap.json`, exits with an error if `package-lock.json` and `package.json` disagree, removes existing `node_modules`, and never writes to `package.json` or lock files. Source: https://docs.npmjs.com/cli/v8/commands/npm-ci/

### Testing Requirements

Minimum validation before review:

```powershell
pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1
npm ci --ignore-scripts
git diff --check -- .github/workflows/release.yml .releaserc.json package.json package-lock.json tools docs/dev/release-runbook.md CONTRIBUTING.md tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/15-1-release-edge-case-preflight-hardening.md
```

Run this when the release workflow or CI inventory tests change:

```powershell
dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~CiTestInventoryTests"
```

Additional probes to record when relevant:

- A stale `v<nextVersion>` tag fixture fails before `npx semantic-release` publish work starts.
- Exact tag fixtures cover no tag, local-only tag, remote-only tag, and local+remote tag without matching similarly prefixed tags.
- A release-eligible commit message without skip tokens still runs the release job.
- A merge/squash message containing quoted `[skip ci]` follows the documented outcome.
- Skip-token fixtures cover unquoted, quoted, escaped, multiline body-only, and no-bracket cases.
- `package-lock.json` is tracked and `npm ci --ignore-scripts` succeeds from a clean dependency tree or isolated worktree without relying on existing `node_modules`.
- Targeted deferred IDs `S11-FC`, `12.1-RV3`, and `12.1-RV4` each have structured status evidence or rationale.
- `git diff --name-only` or equivalent review confirms the final diff stays inside the story's allowed files and excludes submodule pointer changes.

## Project Structure Notes

- This is a release tooling, workflow, and governance story. Runtime application code is out of scope.
- Prefer existing PowerShell tooling and xUnit/Shouldly CI inventory tests over adding a new framework.
- The repository has root-level submodules. This story must not initialize/update nested submodules and must not include submodule pointer changes.

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 15 and Story 15.1 acceptance criteria.
- `_bmad-output/implementation-artifacts/deferred-work.md` - target deferred IDs `S11-FC`, `12.1-RV3`, and `12.1-RV4`.
- `_bmad-output/implementation-artifacts/12-1-first-release-path-validation.md` - original release-path evidence and deferred skip/package-lock findings.
- `_bmad-output/implementation-artifacts/14-2-release-pipeline-audit-hardening.md` - current release workflow/package validation hardening baseline.
- `_bmad-output/implementation-artifacts/14-5-deferred-register-governance-and-sprint-status-hygiene.md` - structured deferred-work schema and sprint-status guidance.
- `.github/workflows/release.yml` - current release workflow and skip condition.
- `.releaserc.json` - semantic-release `tagFormat` and plugin chain.
- `package.json` and `package-lock.json` - release tooling dependencies and lockfile.
- `tools/validate-release-packages.ps1` - canonical package inventory validator.
- `docs/dev/release-runbook.md` - operator release evidence and recovery guidance.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` - existing release workflow contract assertions.
- GitHub Actions skip workflow docs: https://docs.github.com/en/actions/how-tos/manage-workflow-runs/skip-workflow-runs
- semantic-release configuration docs: https://semantic-release.gitbook.io/semantic-release/usage/configuration
- semantic-release troubleshooting docs: https://semantic-release.gitbook.io/semantic-release/support/troubleshooting
- npm `ci` docs: https://docs.npmjs.com/cli/v8/commands/npm-ci/

## Dev Agent Record

### Agent Model Used

GPT-5

### Implementation Plan

- Add a release-only PowerShell preflight that obtains the next release version from semantic-release dry-run output, applies the configured tag format, and checks exact local and remote tag refs before publish-capable work starts.
- Remove the release workflow's partial job-level skip parser and document GitHub's native full-message skip behavior as an accepted release-maintainer risk.
- Preserve the existing package-lock contract by verifying `package-lock.json` tracking and `npm ci --ignore-scripts`, without changing package dependencies or lockfile content.
- Close the targeted deferred IDs with Story 14.5 structured fields and focused tests.

### Debug Log References

- Pre-dev hardening preflight passed at JSON timestamp `2026-05-12T17:30:42Z` with all checks green and `working tree cleanliness` reporting `0 dirty paths`.
- Story selection chose `15-1-release-edge-case-preflight-hardening` because `ready_count` was `0`, below the target of `5`, and this was the first backlog story in sprint-status order.
- `/bmad-create-story 15-1-release-edge-case-preflight-hardening` context gathering loaded Epic 15 planning, sprint status, root project context, Story 12.1, Story 14.2, Story 14.5, Epic 14 retrospective, current deferred-work entries, release workflow/config/tooling, package files, CI inventory tests, release runbook, recent git history, and current official GitHub Actions, semantic-release, and npm `ci` documentation.
- Party-mode review ran on 2026-05-12 after preflight JSON timestamp `2026-05-12T19:37:07Z` failed only `working tree cleanliness` for planning artifacts outside BMAD story-operation paths; classified as a soft working-tree warning per the recurring-job rules.
- 2026-05-13 red phase: `python -m unittest discover -s tests/tooling/release_preflight -p "*_test.py"` failed because `tools/release-preflight.ps1` did not exist; `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~CiTestInventoryTests"` failed on the missing release preflight step and the existing head-commit skip condition.
- 2026-05-13 focused validation passed: release preflight fixtures 6/6, `CiTestInventoryTests` 47/47, `pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1`, and `npm ci --ignore-scripts`.
- 2026-05-13 `git diff --check -- .github/workflows/release.yml .releaserc.json package.json package-lock.json tools docs/dev/release-runbook.md CONTRIBUTING.md tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/15-1-release-edge-case-preflight-hardening.md` passed with only LF-to-CRLF working-copy warnings.
- 2026-05-13 solution-level `dotnet test Hexalith.Memories.slnx --no-restore` was attempted; unit/non-Docker projects passed, but `Hexalith.Memories.IntegrationTests` failed 107 tests because the environment cannot locate the Dapr CLI.
- 2026-05-13 release non-Docker lane passed via `pwsh -NoLogo -NoProfile -File ./tools/test-release.ps1`: Contracts 468/468, Server 1543/1543, CLI 334/334, MCP 76/76, EventStore 84/84.

### Completion Notes List

- Story context created on 2026-05-12.
- Scope is limited to release-edge preflight decisions, skip-token guard/documentation, package-lock/fresh-clone proof, deferred-work bookkeeping, and focused validation.
- Runtime source, package metadata unrelated to release tooling, CI workflows other than `release.yml`, and submodules are forbidden by default.
- No submodule state was touched.
- Party-mode review hardened the story with exact stale-tag, skip-token, npm proof, deferred-work schema, operator-message, and file-scope validation expectations.
- Implemented `tools/release-preflight.ps1` and wired it before `npx semantic-release`; it uses semantic-release dry-run output for the next version and checks exact local and remote tag refs.
- Removed the release job's partial `github.event.head_commit.message` skip parser and documented GitHub-native full-message skip semantics as an accepted release-maintainer risk.
- Verified `package-lock.json` and `package.json` are tracked, release workflow restore uses `npm ci`, and `npm ci --ignore-scripts` succeeds without lockfile changes.
- Added structured deferred-work dispositions: `S11-FC` resolved, `12.1-RV3` accepted, and `12.1-RV4` resolved. `12.1-RV5` and unrelated entries were not swept.
- Final file-scope check found changes only inside the story's allowed file list. `_bmad-output/process-notes/predev-preflight-*.json` snapshots (including `predev-preflight-latest.json`) are recurring-job output from `jobs/preflight-predev-hardening.py`, are not story-authored changes, and are excluded from preflight working-tree cleanliness gates via the documented `:(exclude)_bmad-output/process-notes/predev-preflight-*.json` rule. No submodule pointers were touched.

### File List

- `.github/workflows/release.yml`
- `_bmad-output/implementation-artifacts/15-1-release-edge-case-preflight-hardening.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/dev/release-runbook.md`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `tests/tooling/release_preflight/release_preflight_test.py`
- `tools/release-preflight.ps1`

### Party-Mode Review

- Date/time: `2026-05-12T21:43:54+02:00`
- Selected story key: `15-1-release-edge-case-preflight-hardening`
- Command/skill invocation used: `/bmad-party-mode 15-1-release-edge-case-preflight-hardening; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - S11-FC needed an explicit release-owner decision path and exact tag-ref semantics instead of ambiguous "reassess" wording.
  - 12.1-RV3 needed a precise skip-token boundary for quoted, escaped, subject/body, and multiline merge/squash messages.
  - 12.1-RV4 needed reproducible clean-checkout npm proof rather than ad hoc local verification.
  - Deferred-work dispositions needed Story 14.5 schema conformance and mutually exclusive `resolved`, `accepted`, and `carried-forward` semantics.
  - Any release preflight tests needed fixture isolation from real tags/remotes, actionable failure messages, and a final file-scope guard.
- Changes applied:
  - Added `Party-Mode Review Clarifications - 2026-05-12` with exact stale-tag, skip-token, npm proof, deferred-work schema, operator-message, scope, and fixture-isolation guidance.
  - Expanded additional validation probes for exact tag fixtures, skip-token fixtures, isolated npm restore proof, structured deferred-work evidence, and final file-scope review.
- Findings deferred:
  - Cross-provider CI skip-token semantics remain out of scope unless this repository adds another release CI system.
  - Full semantic-release dry-run before every release remains a story implementation decision; it is not required if an isolated preflight or explicit carry-forward rationale is chosen.
  - Broad historical tag hygiene and unrelated `12.1-RV5` contributor-discoverability work remain out of scope unless naturally closed by the release-edge implementation.
- Final recommendation: `ready-for-dev`

### Change Log

- 2026-05-12: Created Story 15.1 and promoted it from `backlog` to `ready-for-dev`.
- 2026-05-12: Party-mode review completed; added release-edge pre-dev clarifications and validation probes.
- 2026-05-13: Implemented release preflight, skip-token documentation/contract tests, package-lock proof, deferred-work dispositions, and focused validation; moved story to `review`.
- 2026-05-13: Adversarial 3-layer code review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) completed; findings appended below.
- 2026-05-13: Review close-out — 12 patches applied (P1 git remote get-url exit-code allowlist; P2 ErrorRecord/stdout stream separation in `Invoke-GitCommand`; P3 step-level `timeout-minutes: 10`; P4 `-NextVersion` semver shape validation; P5 CRLF-tolerant `release:` header match; P6 positive dry-run regex + leading-`v` rejection fixtures; P7 trailing-CRLF trim on `ls-remote` ref field; P8 force `$LASTEXITCODE = 0` baseline; P9 truth-claim fix on predev-preflight working-tree files; P10 12.1-RV4 fresh-clone evidence granularity; P11 multi-distinct-version guard in `Get-NextReleaseVersionFromDryRun`; P12 new `ReleaseWorkflow_NoStep_UsesHeadCommitSkipCondition` step-level contract). 16 carry-forward review items recorded as `15.1-RV1`..`15.1-RV16` in `deferred-work.md`. Focused validation: release preflight fixtures 9/9 (was 6/6, +3 positive/multi-version/leading-v); CiTestInventoryTests 48/48 (was 47/47, +1 step-level skip guard); `tools/validate-release-packages.ps1` passed; `git diff --check` clean (LF/CRLF warnings only). Moved story to `done`.

### Review Findings

3-layer adversarial code review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) on 2026-05-13. Acceptance Auditor verdict: **READY FOR DONE WITH PATCHES** — all 4 ACs PASS; 2 minor evidence-and-bookkeeping gaps and several edge-case hardening opportunities surfaced.

- [x] [Review][Decision] D1 — Resolved 2026-05-13: expanded the contract test to also assert no step under `jobs.release` re-introduces the partial skip parser. Added `ReleaseWorkflow_NoStep_UsesHeadCommitSkipCondition` at `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:223-235`. CiTestInventoryTests now 48/48 (was 47, +1).

- [x] [Review][Patch] P1 — `git remote get-url` for a missing remote returns exit code `128` on current git versions (`die("No such remote: ...")`), but the script's `AllowedExitCodes @(0, 2)` will only tolerate `2`. A missing remote would throw instead of taking the documented "skip remote check" warning branch. [`tools/release-preflight.ps1:115`]

- [x] [Review][Patch] P2 — `Invoke-GitCommand` merges `stderr` and `stdout` via `2>&1`; `Test-RemoteTagCollision` then iterates the merged stream as if every line were a `ls-remote` data line. `ErrorRecord` objects (e.g., HTTPS warnings emitted to stderr by `ls-remote`) get string-coerced and split, which can either spuriously match a tag ref or hide a legitimate peeled-ref line. Filter the output to plain-string `OutputType` entries (or capture stderr separately) before the field split. [`tools/release-preflight.ps1:19-42, 121-138`]

- [x] [Review][Patch] P3 — The `Run release preflight` workflow step has no `timeout-minutes`; the only backstop is the job-level 30-minute timeout. A hung `npm run release:dry-run` or `ls-remote` would consume the entire job budget instead of failing loud. Add `timeout-minutes: 10` to the preflight step. [`.github/workflows/release.yml:84-88`]

- [x] [Review][Patch] P4 — `-NextVersion` is taken verbatim and pasted into `tagFormat`. A caller passing `v1.2.3` (with a leading `v`) produces `refs/tags/vv1.2.3`; a non-semver value passes silently as long as it is non-whitespace. Validate against the same semver shape used in the dry-run regex before composing the tag ref. [`tools/release-preflight.ps1:141-152`]

- [x] [Review][Patch] P5 — `GetReleaseWorkflowJobScalar` uses `if (line == "  release:")` to locate the job header. When `release.yml` is checked out with CRLF endings (the patch already triggers the `LF will be replaced by CRLF` warning), the line becomes `"  release:\r"` and the helper falls off the end with `insideReleaseJob = false`, failing the `ShouldBeTrue` invariant instead of returning `null`. Use `line.TrimEnd() == "  release:"` (or split on `\n` after a normalize step) so the contract test is CRLF-tolerant. [`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:767-799`]

- [x] [Review][Patch] P6 — The Python fixture suite has positive tests for the `-NextVersion` and the no-release dry-run path, but the live regex-parse path (`Get-NextReleaseVersionFromDryRun` matching `The next release version is X.Y.Z`) has no fixture. Add one test that feeds a `-SemanticReleaseDryRunOutputPath` containing a positive `The next release version is 1.2.3` line and asserts the preflight resolves and reports `No stale release tag found for refs/tags/v1.2.3`. [`tests/tooling/release_preflight/release_preflight_test.py`]

- [x] [Review][Patch] P7 — In `Test-RemoteTagCollision`, the line-by-line loop splits on `\s+` and compares `$fields[1]` to `$TagRef`. On a Windows runner / CRLF-emitting remote, `$fields[1]` can carry a trailing `\r`, breaking the equality check. Add a `.TrimEnd()` (or normalize line endings up front) so the comparison is robust to CRLF residue. [`tools/release-preflight.ps1:128-137`]

- [x] [Review][Patch] P8 — Before the first `Invoke-GitCommand` runs, `$LASTEXITCODE` may carry a stale value from a prior step in the same pwsh session. If the very first git invocation fails to launch (PATH or permission), `$LASTEXITCODE` keeps its stale value and the wrapper's `if ($AllowedExitCodes -notcontains $exitCode)` may incorrectly accept it. Set `$global:LASTEXITCODE = 0` at the top of the `try` block. [`tools/release-preflight.ps1:141`]

- [x] [Review][Patch] P9 — Completion Notes claims "Pre-existing process-note working-tree files remain unrelated and were not modified." The tracked diff modifies `_bmad-output/process-notes/predev-preflight-latest.json` (size 39760 → 39787; timestamp/dirty_path_count updated). Reword the bullet to acknowledge recurring-job output drift (not a story-authored change) so the truth-claim matches the working tree. [`_bmad-output/implementation-artifacts/15-1-release-edge-case-preflight-hardening.md:237`]

- [x] [Review][Patch] P10 — `12.1-RV4` Evidence and Debug Log References say `npm ci --ignore-scripts` was run, but do not record the working directory of the isolated worktree, the no-existing-`node_modules` assumption, or the post-run "tracked files stayed clean" check the party-mode review demanded. Add a one-paragraph evidence block naming the working directory and the post-run `git status -- package-lock.json package.json` outcome. [`_bmad-output/implementation-artifacts/deferred-work.md:80-89` + Debug Log References]

- [x] [Review][Patch] P11 — `Get-NextReleaseVersionFromDryRun` collects every `The next release version is X.Y.Z` match in the dry-run output and silently picks the last one. If semantic-release ever logs two different versions (e.g., a candidate then a final), the script will use whichever appears last with no diagnostic. If two distinct version values are present, throw with both candidates named so the operator can investigate. [`tools/release-preflight.ps1:86-102`]

- [x] [Review][Defer] W1 — No retry/backoff on transient `ls-remote` network failures; a DNS hiccup turns the preflight into a hard abort. [`tools/release-preflight.ps1:115-138`] — deferred as `15.1-RV1`.
- [x] [Review][Defer] W2 — Dry-run regex hard-codes the English phrase "The next release version is" — fragile to semantic-release i18n or output reformatting. [`tools/release-preflight.ps1:86-102`] — deferred as `15.1-RV2`.
- [x] [Review][Defer] W3 — Final `catch` block uses `Write-Error -Message $_.Exception.Message`, losing inner-exception and stack-trace context for CI operators. [`tools/release-preflight.ps1:158-167`] — deferred as `15.1-RV3`.
- [x] [Review][Defer] W4 — `CiTestInventoryTests` workflow-string assertions use exact `ShouldBe("npm ci")` / `ShouldBe("./tools/release-preflight.ps1")` / step-name equality. Cosmetic edits (e.g., `npm ci --ignore-scripts`, renamed step) would break the test without a real contract violation. [`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:108-133`] — deferred as `15.1-RV4`.
- [x] [Review][Defer] W5 — Python `tempfile.TemporaryDirectory` cleanup can raise `PermissionError` on Windows if git keeps an index lock. Use `ignore_cleanup_errors=True` (Py 3.10+). [`tests/tooling/release_preflight/release_preflight_test.py`] — deferred as `15.1-RV5`.
- [x] [Review][Defer] W6 — Python `Path | None` union syntax requires Python 3.10+; project minimum version is not asserted at the top of the test file. [`tests/tooling/release_preflight/release_preflight_test.py:134`] — deferred as `15.1-RV6`.
- [x] [Review][Defer] W7 — `subprocess.run(..., text=True)` on a Windows codepage other than UTF-8 can raise `UnicodeDecodeError` when pwsh stderr contains non-ASCII. Pass `encoding='utf-8', errors='replace'`. [`tests/tooling/release_preflight/release_preflight_test.py:107-122`] — deferred as `15.1-RV7`.
- [x] [Review][Defer] W8 — `_init_repo` calls `git init` with no `--initial-branch`; behavior depends on `init.defaultBranch` of the host environment. Pass `--initial-branch=main` explicitly. [`tests/tooling/release_preflight/release_preflight_test.py:127`] — deferred as `15.1-RV8`.
- [x] [Review][Defer] W9 — Test runner hardcodes `pwsh`; no `skipUnless(shutil.which('pwsh'), ...)` guard for environments without PowerShell 7. [`tests/tooling/release_preflight/release_preflight_test.py:107`] — deferred as `15.1-RV9`.
- [x] [Review][Defer] W10 — Release-day checklist in the runbook was renumbered to 17 steps (was 13). Other docs / CONTRIBUTING / story references to specific step numbers may now be stale. [`docs/dev/release-runbook.md:283-301`] — deferred as `15.1-RV10`.
- [x] [Review][Defer] W11 — `S11-FC` re-open trigger names `tools/release-preflight.ps1` by path. A rename or relocation makes the trigger silently invalid. [`_bmad-output/implementation-artifacts/deferred-work.md:54-65`] — deferred as `15.1-RV11`.
- [x] [Review][Defer] W12 — `12.1-RV3` accepted with explicit defer-by date `2026-08-13`; no automated reminder that surfaces expired accepted entries. [`_bmad-output/implementation-artifacts/deferred-work.md:67-78`] — deferred as `15.1-RV12`.
- [x] [Review][Defer] W13 — `git show-ref --verify --quiet $TagRef` allowed exit codes are `(0, 1)`. If the repository state forces git to return `128` (e.g., corrupt object store, broken ref), the wrapper throws a generic git-failed message instead of a clear "ref state probe failed" diagnostic. [`tools/release-preflight.ps1:107-110`] — deferred as `15.1-RV13`.
- [x] [Review][Defer] W14 — Some remotes return only peeled refs (`refs/tags/v1.2.3^{}`) without an unpeeled entry; the current loop accepts either, but the contract is not test-fixtured. [`tools/release-preflight.ps1:128-137`] — deferred as `15.1-RV14`.
- [x] [Review][Defer] W15 — `Resolve-Path -LiteralPath $RepositoryPath` throws a cryptic `Cannot find path` error when `-RepositoryPath` does not exist. Wrap with a clear pre-check. [`tools/release-preflight.ps1:141`] — deferred as `15.1-RV15`.
- [x] [Review][Defer] W16 — `GetReleaseWorkflowJobScalar` parser depends on exactly 4-space indentation. Any future `release.yml` reformat (2-space, tabs) silently breaks the contract test. [`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:767-799`] — deferred as `15.1-RV16`.

Dismissed as noise / false-positive (not written into the story or deferred register):
- "Test bodies are stubs (`...`)" — artifact of how the review prompt summarized the diff; actual `.py` file has full implementations.
- "GITHUB_TOKEN is set in workflow but never used by the script" — it is consumed by `npm run release:dry-run` → semantic-release for the repository push-permission check.
- "Removing `if:` allows release on every push event including non-push triggers" — the workflow is push-only per Dev Notes; GitHub-native skip handling covers the trigger set.
- "Multi-token `tagFormat` rejection" — semantic-release explicitly requires the `${version}` token exactly once.
- "Stale-tag exact-ref match is just default `git show-ref` behavior, not a real design choice" — rhetorical; the contract is the protection regardless.
- Several stylistic / framing critiques and infrastructure assumptions covered by basic CI invariants.

## Story Completion Status

Story context created and ready for implementation. Status set to `ready-for-dev`.
