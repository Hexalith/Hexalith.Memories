# Story 15.1: Release Edge-Case Preflight Hardening

Status: ready-for-dev

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

- [ ] Task 1 - Reassess stale-tag collision handling (AC: 1, 4)
  - [ ] Read `.releaserc.json`, `package.json`, `.github/workflows/release.yml`, `docs/dev/release-runbook.md`, and the current `S11-FC` entry in `deferred-work.md` before changing anything.
  - [ ] Decide whether stale-tag protection belongs in a new preflight script, an inline workflow step before `npx semantic-release`, a release-runbook acceptance decision, or a refreshed carried-forward entry.
  - [ ] If implementing a preflight, compare the tag that semantic-release would create against remote tags without running prepare/publish side effects. Do not invent custom version calculation unless tests prove it matches the semantic-release commit analyzer for this repository.
  - [ ] If accepting or carrying forward the risk, record why the cost of `semantic-release --dry-run` or a custom preflight still does not justify a workflow change and set a fresh trigger/defer-by date.

- [ ] Task 2 - Guard or document skip-CI release bypass behavior (AC: 2, 4)
  - [ ] Review the current release workflow job-level condition: `!contains(github.event.head_commit.message, '[skip ci]') && !contains(github.event.head_commit.message, '[skip actions]')`.
  - [ ] Decide whether the release job should keep this condition, narrow it, replace it with a repository-owned skip predicate, or document an accepted risk for merge/squash messages that quote skip tokens.
  - [ ] Add focused coverage in `CiTestInventoryTests` if workflow behavior changes or if a parser/guard contract is added.
  - [ ] Update `docs/dev/release-runbook.md` or `CONTRIBUTING.md` so maintainers know whether `[skip ci]` may appear in PR titles/bodies, squash messages, revert text, or quoted examples.

- [ ] Task 3 - Confirm package-lock and fresh-clone release restore behavior (AC: 3, 4)
  - [ ] Verify `package-lock.json` is tracked, matches the root `package.json`, and is used by the release workflow through `npm ci`.
  - [ ] Run or script a fresh-clone-style package restore check that does not rely on an existing `node_modules` directory. A local `npm ci --ignore-scripts` or a temporary copy is acceptable if it proves lock/package consistency without mutating tracked files.
  - [ ] If the current workflow already satisfies `12.1-RV4`, close it with evidence. If not, add the smallest workflow/tooling/doc change that makes the failure mode loud before semantic-release.
  - [ ] Do not update package dependencies or rewrite `package-lock.json` unless the check proves it is stale and the story explicitly records why.

- [ ] Task 4 - Update deferred-work dispositions (AC: 1-4)
  - [ ] Add a Story 15.1 rollup section to `_bmad-output/implementation-artifacts/deferred-work.md` or update the existing structured entries for `S11-FC`, `12.1-RV3`, and `12.1-RV4`.
  - [ ] Use the Story 14.5 schema fields exactly: `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, and either `Evidence` or `Rationale`.
  - [ ] Do not remove historical context. Mark each targeted ID `resolved`, `accepted`, or `carried-forward` with concrete evidence or rationale.
  - [ ] Do not sweep `12.1-RV5` or unrelated release/governance entries unless a touched file naturally closes them and the story records the reason.

- [ ] Task 5 - Validate release-edge changes (AC: 1-4)
  - [ ] Run `pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1`.
  - [ ] Run `npm ci --ignore-scripts` or the selected fresh-clone equivalent and record the result.
  - [ ] If `.github/workflows/release.yml` or `CiTestInventoryTests.cs` changes, run `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~CiTestInventoryTests"`.
  - [ ] If a new release preflight script is added, run its focused positive and negative tests, including a stale-tag collision fixture and a no-release/no-next-version fixture when relevant.
  - [ ] Run `git diff --check -- .github/workflows/release.yml .releaserc.json package.json package-lock.json tools docs/dev/release-runbook.md CONTRIBUTING.md tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/15-1-release-edge-case-preflight-hardening.md`.

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

### Debug Log References

- Pre-dev hardening preflight passed at JSON timestamp `2026-05-12T17:30:42Z` with all checks green and `working tree cleanliness` reporting `0 dirty paths`.
- Story selection chose `15-1-release-edge-case-preflight-hardening` because `ready_count` was `0`, below the target of `5`, and this was the first backlog story in sprint-status order.
- `/bmad-create-story 15-1-release-edge-case-preflight-hardening` context gathering loaded Epic 15 planning, sprint status, root project context, Story 12.1, Story 14.2, Story 14.5, Epic 14 retrospective, current deferred-work entries, release workflow/config/tooling, package files, CI inventory tests, release runbook, recent git history, and current official GitHub Actions, semantic-release, and npm `ci` documentation.
- Party-mode review ran on 2026-05-12 after preflight JSON timestamp `2026-05-12T19:37:07Z` failed only `working tree cleanliness` for planning artifacts outside BMAD story-operation paths; classified as a soft working-tree warning per the recurring-job rules.

### Completion Notes List

- Story context created on 2026-05-12.
- Scope is limited to release-edge preflight decisions, skip-token guard/documentation, package-lock/fresh-clone proof, deferred-work bookkeeping, and focused validation.
- Runtime source, package metadata unrelated to release tooling, CI workflows other than `release.yml`, and submodules are forbidden by default.
- No submodule state was touched.
- Party-mode review hardened the story with exact stale-tag, skip-token, npm proof, deferred-work schema, operator-message, and file-scope validation expectations.

### File List

- `_bmad-output/implementation-artifacts/15-1-release-edge-case-preflight-hardening.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

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

## Story Completion Status

Story context created and ready for implementation. Status set to `ready-for-dev`.
