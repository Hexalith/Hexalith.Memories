# Story 12.1: First Release Path Validation

Status: done

Story Key: 12-1-first-release-path-validation
Epic: 12 - First Release & Operations Foundation
Created: 2026-04-30

## Story

As a maintainer,
I want the first real release to nuget.org to run end-to-end with the Epic 11 infrastructure,
so that the release path is proven against a real publish event before any further feature investment.

## Current State Snapshot

This story was planned before the first publish ran. Current evidence shows the release path has already produced real artifacts, but the current `main` workspace still lacks the story/runbook closeout and GitHub reports `main` branch protection is not configured.

- Local tags exist for `v1.0.0`, `v1.1.0`, and `v1.2.0`; `v1.2.0` points at release commit `f4b5038`.
- GitHub Release `v1.2.0` exists and was published on 2026-04-30 05:04:38 UTC with seven `.nupkg` assets.
- NuGet flat-container API reports versions `1.0.0`, `1.1.0`, and `1.2.0` for all seven approved packages as of 2026-04-30.
- `gh api repos/Hexalith/Hexalith.Memories/branches/main/protection` currently returns `404 Branch not protected`.
- `docs/dev/release-runbook.md` does not exist on current `main`.
- Remote branch `origin/codex/update-memories-release-path` contains an already-completed implementation of this story, including a runbook. Inspect it before reimplementing, but do not blindly overwrite current `main`; reconcile deliberately.

Implementation must therefore treat 12.1 as a release-path reconciliation and hardening story: preserve real release evidence, apply or verify branch protection, validate NuGet/GitHub artifacts, and write the observed runbook. Do not force an artificial new release just because the original story wording expected a future first publish.

## Acceptance Criteria

1. Given Epic 11 retrospective action A1 has a documented branch-protection contract in `docs/dev/branch-protection.md`, when a maintainer applies repository protection for `main`, then GitHub enforces pull requests before merge, at least one approval, required status checks `build`, `test-unit-contract`, and `integration-fast`, up-to-date branches before merge (`required_status_checks.strict: true`), administrator enforcement where available, force-push blocking, and branch-deletion blocking. If stale-approval dismissal is not enabled, the runbook must state that choice and why.
2. Given the current GitHub API reports `main` branch protection as `404 Branch not protected`, when this story is completed, then the story completion notes and runbook include dated before/after evidence for branch protection. Before treating the 404 as the final state, verify repository owner/name, default branch, authenticated `gh` scope, and branch name. If the developer cannot apply the setting due to permissions, the story must remain blocked or explicitly mark `External Action Pending`; it must not be moved to `done`.
3. Given Epic 11 retrospective action A2 requires a scoped `NUGET_API_KEY` repository secret, when release validation is documented, then `release.yml` is shown to reference `secrets.NUGET_API_KEY`, no secret value is logged or copied into repo files, and the observed successful release run proves the credential was available to semantic-release/publish.
4. Given release run evidence already produced `v1.2.0`, when the maintainer validates the release path, then the runbook records the historically observed sequence: push to `main`, `release.yml` execution, semantic-release version calculation, package pack/validation, NuGet publish, tag creation, GitHub Release creation, and `[skip ci]` release commit behavior. This is an observation/documentation requirement, not permission to perform a new release. The story must not create tags, run a real `dotnet nuget push`, mutate package inventory, merge a release-triggering commit, or trigger a new publish unless Jerome explicitly approves it.
5. Given the approved package inventory in `tools/release-packages.json`, when the released version is inspected, then all seven packages are present on nuget.org at the same version, have corresponding GitHub Release assets, and match the approved package set: `Hexalith.Memories.Contracts`, `Hexalith.Memories.Client.Rest`, `Hexalith.Memories.Redis`, `Hexalith.Memories.Cli`, `Hexalith.Memories.Mcp`, `Hexalith.Memories.EventStore`, and `Hexalith.Memories.Telemetry`.
6. Given package validation is part of the release contract, when the release evidence is captured, then `tools/validate-release-packages.ps1` remains the canonical verifier for package metadata, package IDs, README packing, and internal dependency version equality. Do not replace it with manual inspection.
7. Given the first release has succeeded, then `docs/dev/release-runbook.md` exists and documents the repeatable second-release path using observed evidence, not speculative instructions. The runbook separates verified observations, inferred workflow, unresolved gaps, and recommended next steps, and includes an evidence table covering GitHub tags/releases, NuGet package availability, branch-protection status, and prior branch/runbook inspection.
8. Given `origin/codex/update-memories-release-path` contains prior completed work, when the dev agent uses it, then the story completion notes summarize what was reused, what was rejected, and what conflicts with current `main`. The dev agent must inspect via targeted `git show`/`git diff`/`git log` or equivalent and must not merge the branch wholesale.
9. Given branch protection is required before the release path can be called repeatable, when this story documents the release path, then it includes an explicit semantic-release + branch-protection compatibility decision: either confirm `@semantic-release/git` can still push the release commit safely under the chosen protection model, or document the expected failure mode and create/defer a follow-up release-strategy item without weakening branch protection silently.
10. Given Epic 12 deliberately defers Phase 2 feature work, then this story does not modify runtime source code, public API contracts, MCP behavior, EventStore integration, release package inventory, tags, or published packages unless a release-path defect is found and explicitly documented with a `Scope-Override:` rationale or a follow-up story.

## Tasks / Subtasks

- [x] Task 1 - Capture immutable release evidence (AC: 3, 4, 5)
  - [x] Record GitHub Actions release run URL and run ID for `v1.2.0`.
  - [x] Record the `v1.2.0` tag, release commit `f4b5038`, release commit message `chore(release): 1.2.0 [skip ci]`, and source commit.
  - [x] Record the GitHub Release URL and seven asset names.
  - [x] Query the NuGet flat-container API for each approved package and record the latest version observed.
  - [x] Produce an evidence table in the story completion notes or runbook with columns: artifact, source checked, observed result, implication.
  - [x] Inspect `origin/codex/update-memories-release-path` for existing 12.1 evidence/runbook work and reuse only the parts that still match current remote truth.
  - [x] Summarize prior-branch decisions in three buckets: reused, rejected, and deferred/conflicting.

- [x] Task 2 - Apply or verify branch protection (AC: 1, 2)
  - [x] Verify repository owner/name, default branch, authenticated `gh` scope, and target branch before interpreting a branch-protection 404 as missing protection.
  - [x] Follow `docs/dev/branch-protection.md` for `main`.
  - [x] Require pull request before merge.
  - [x] Require at least one approval.
  - [x] Require status checks `build`, `test-unit-contract`, and `integration-fast`.
  - [x] Require branches to be up to date before merge (`required_status_checks.strict: true`).
  - [x] Include administrators in enforcement where repository settings allow it.
  - [x] Block direct pushes to `main`.
  - [x] Block force pushes and branch deletion.
  - [x] Record whether stale approval dismissal is enabled; if disabled, document the rationale.
  - [x] Re-run `gh api repos/Hexalith/Hexalith.Memories/branches/main/protection` and capture the resulting settings.
  - [x] If permissions are insufficient, update the story completion notes with `External Action Pending` and do not claim AC #1/#2 complete.

- [x] Task 3 - Verify release workflow and secret usage (AC: 3, 4)
  - [x] Confirm `.github/workflows/release.yml` still runs only on pushes to `main`.
  - [x] Confirm semantic-release receives `GITHUB_TOKEN` and `NUGET_API_KEY` through environment variables only.
  - [x] Confirm `.releaserc.json` still invokes `tools/pack-release.ps1` in `prepareCmd` and `tools/publish-nuget.ps1` in `publishCmd`.
  - [x] Confirm the release commit skip behavior is intentional and documented in the runbook.
  - [x] Validate release behavior through no-new-release evidence only: prior run logs, current workflow/config inspection, local validation scripts, GitHub Release metadata, and NuGet package metadata.
  - [x] Do not merge or create a release-triggering commit during this story unless Jerome explicitly approves a live release validation.
  - [x] Document whether `@semantic-release/git` can push its `CHANGELOG.md` release commit under the applied branch-protection model; if not, record the expected failure point and create/defer a release-strategy follow-up.

- [x] Task 4 - Validate package inventory and published artifacts (AC: 5, 6)
  - [x] Run `./tools/validate-release-packages.ps1`.
  - [x] If release packages are available locally or downloaded from GitHub Release, run `./tools/validate-release-packages.ps1 -PackageDirectory <dir> -Version 1.2.0`.
  - [x] Verify GitHub Release assets match `tools/release-packages.json` exactly for `Hexalith.Memories.Contracts`, `Hexalith.Memories.Client.Rest`, `Hexalith.Memories.Redis`, `Hexalith.Memories.Cli`, `Hexalith.Memories.Mcp`, `Hexalith.Memories.EventStore`, and `Hexalith.Memories.Telemetry`.
  - [x] Verify all approved packages are present on nuget.org at versions `1.0.0`, `1.1.0`, and `1.2.0`, with `1.2.0` as the current latest version observed.
  - [x] Do not add or remove packages from `tools/release-packages.json` in this story.

- [x] Task 5 - Write the observed release runbook (AC: 4, 7)
  - [x] Add `docs/dev/release-runbook.md`.
  - [x] Include prerequisites: branch protection, scoped `NUGET_API_KEY`, clean `main`, conventional commit, package inventory validation.
  - [x] Include the observed first-release evidence from `v1.2.0`.
  - [x] Include the second-release checklist.
  - [x] Include recovery notes for failed release runs and partial publish, but keep implementation of partial-publish alerting in Story 12.5.
  - [x] Include recovery notes for semantic-release git-plugin failure after packages publish, branch-protection misconfiguration, and stale tag collision.
  - [x] Explicitly label verified observations, inferred workflow, unresolved gaps, and recommendations.
  - [x] Do not invent release steps, approval gates, tag behavior, or CI commands; label anything uncertain as unknown or recommended.

- [x] Task 6 - Close status honestly (AC: 1-8)
  - [x] Update this story's Dev Agent Record with commands run and links/evidence captured.
  - [x] If branch protection is still not enforced, leave the story blocked or in progress with `External Action Pending`.
  - [x] If all ACs are satisfied, update sprint status through the BMad workflow; do not manually jump from `ready-for-dev` to `done` without dev-story/review evidence.
  - [x] Confirm no new tags were created, no NuGet publish was run locally, and no release package inventory mutation occurred unless explicitly approved.
  - [x] Confirm no release-triggering commit was merged unless explicitly approved.

### Review Findings

- [x] [Review][Patch] Remove unintended `Hexalith.Memories.sln` from the story change set [`Hexalith.Memories.sln`:1]
- [x] [Review][Patch] Document stale approval dismissal state and rationale in the runbook [`docs/dev/release-runbook.md`:80]
- [x] [Review][Patch] Add dated before-protection evidence to the runbook [`docs/dev/release-runbook.md`:80]

## File Scope

Allowed files for this story:

- `docs/dev/release-runbook.md` - NEW. Main deliverable.
- `docs/dev/branch-protection.md` - UPDATE only if recording applied-date/evidence or clarifying the current GitHub settings sequence.
- `CONTRIBUTING.md` - UPDATE only for a cross-link to the runbook; the forbidden-default-tolerances checklist belongs to Story 12.2.
- `_bmad-output/implementation-artifacts/12-1-first-release-path-validation.md` - UPDATE Dev Agent Record and completion notes.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through the BMad workflow when story state changes.

Read/verify only unless a release-path defect is found and documented:

- `.github/workflows/release.yml`
- `.github/workflows/ci.yml`
- `.releaserc.json`
- `package.json`
- `package-lock.json`
- `tools/release-packages.json`
- `tools/pack-release.ps1`
- `tools/validate-release-packages.ps1`
- `tools/publish-nuget.ps1`
- `tools/test-release.ps1`
- `CHANGELOG.md`

Forbidden by default:

- `src/**/*.cs`
- `tests/**/*.cs`
- Package inventory changes
- Tag creation or deletion
- Local or CI-triggered NuGet publish attempts
- Merging any commit to `main` for the purpose of causing a new semantic-release run
- Runtime behavior changes
- MCP, EventStore, Redis, Server, CLI, or Contracts changes

If a forbidden file must change to fix a real release-path defect, stop and either create a follow-up story or add an explicit `Scope-Override:` rationale in the commit/story notes.

## Dev Notes

### Epic Context

Epic 12 exists to prove operations and release readiness before any Phase 2 feature investment. It is driven by the Epic 11 retrospective and Sprint Change Proposal 2026-04-26 Option C. Do not broaden this story into deferred Phase 2 items such as per-tenant LLM configuration, tokenizer-accurate budgets, projection registry, MCP trace-hop proof, or EventStore feature work.

Story 12.1 closes the gap between "release automation exists" and "release path is proven and governable." The first actual release appears to have already occurred, so the main risks are incomplete governance and missing documentation: branch protection is absent in the current GitHub API response, the runbook is missing from current `main`, and sprint status still says this story is backlog until this file is created.

### Current Release Surface

Current files and behaviors to preserve:

- `.github/workflows/release.yml` restores, builds, runs `tools/test-release.ps1`, installs npm tooling, validates package inventory, runs `npx semantic-release`, and uploads release package artifacts.
- `.releaserc.json` uses `@semantic-release/commit-analyzer`, release notes, changelog, exec, GitHub, and git plugins. The exec plugin runs `pack-release.ps1` during prepare and `publish-nuget.ps1` during publish.
- `tools/release-packages.json` is the approved package inventory and currently lists seven published packages plus three non-packable projects.
- `tools/pack-release.ps1` validates inventory, deletes/recreates the output directory, builds the solution once with the release version, packs each approved package with `--no-build`, and validates generated packages.
- `tools/validate-release-packages.ps1` enforces packable/non-packable inventory, required NuGet metadata, packed README presence, case-sensitive package IDs, generated package inventory, released version, and exact internal dependency version lower bounds.
- `tools/publish-nuget.ps1` requires `NUGET_API_KEY`, validates generated packages before publishing, pushes sorted `.nupkg` files to `https://api.nuget.org/v3/index.json`, and uses `--skip-duplicate`.
- `tools/test-release.ps1` intentionally filters `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` as S11-FA. Do not solve S11-FA here; Story 12.6 owns it.

### External Actions and Permissions

Branch protection and repository secrets are maintainer-owned external actions. The developer may be able to inspect GitHub settings with `gh`, but must not claim completion unless the settings are enforced. Current observed state on 2026-04-30:

- `gh api repos/Hexalith/Hexalith.Memories/branches/main/protection` returns `404 Branch not protected`.
- The successful `v1.2.0` release proves a NuGet credential was available during the release run, but GitHub secrets cannot be read back by value. Document presence through workflow behavior and settings metadata only; never expose secret values.
- The branch-protection evidence must be dated and phrased as an observed API result, not as an eternal statement about the repository.
- If branch protection is enabled, explicitly check the interaction with `@semantic-release/git`. A future release may publish packages successfully and then fail when the git plugin tries to push a `CHANGELOG.md` release commit to protected `main`; document the risk or defer a release-strategy follow-up instead of weakening branch protection silently.

### Release Evidence to Capture

Use these commands as starting points:

```powershell
git show --stat --oneline --decorate v1.2.0
git show -s --format='%H%n%h%n%an%n%ae%n%ad%n%s' --date=iso-strict v1.2.0
gh release view v1.2.0 --json tagName,name,publishedAt,url,isDraft,isPrerelease,assets
gh api repos/Hexalith/Hexalith.Memories/branches/main/protection
./tools/validate-release-packages.ps1
git show origin/codex/update-memories-release-path:docs/dev/release-runbook.md
```

NuGet flat-container package-version checks:

```powershell
$ids = @(
    'Hexalith.Memories.Contracts',
    'Hexalith.Memories.Client.Rest',
    'Hexalith.Memories.Redis',
    'Hexalith.Memories.Cli',
    'Hexalith.Memories.Mcp',
    'Hexalith.Memories.EventStore',
    'Hexalith.Memories.Telemetry'
)

foreach ($id in $ids) {
    $url = "https://api.nuget.org/v3-flatcontainer/$($id.ToLowerInvariant())/index.json"
    $versions = (Invoke-RestMethod -Uri $url -TimeoutSec 20).versions
    [pscustomobject]@{ Package = $id; Versions = ($versions -join ', '); Latest = ($versions | Select-Object -Last 1) }
}
```

### Testing Requirements

Minimum validation before completing this story:

- `./tools/validate-release-packages.ps1`
- GitHub API verification for branch protection or explicit `External Action Pending`
- GitHub Release asset inventory check for `v1.2.0`
- NuGet flat-container version check for all seven packages
- Markdown review of `docs/dev/release-runbook.md`
- Prior branch inspection summary: reused, rejected, deferred/conflicting
- Evidence table covering every acceptance criterion
- Semantic-release + branch-protection compatibility decision
- No-new-release validation statement confirming the story used historical evidence and local/read-only checks only, unless Jerome explicitly approved a live release

Do not run a real `dotnet nuget push` locally. Publishing is CI-only per `CONTRIBUTING.md`.

### Latest Technical Information

Web verification performed on 2026-04-30:

- GitHub branch protection supports requiring pull requests, approval counts, required status checks, and push restrictions for protected branches. Project-specific required checks remain `build`, `test-unit-contract`, and `integration-fast`. Source: https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/managing-a-branch-protection-rule
- GitHub Actions `permissions` controls the `GITHUB_TOKEN`; `contents: write` is the relevant permission for creating releases. Source: https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax
- semantic-release stores branch/tag/plugin behavior in configuration files such as `.releaserc.json`, and prior releases need matching tags in release-branch history. Source: https://semantic-release.gitbook.io/semantic-release/usage/configuration
- `dotnet nuget push --skip-duplicate` treats HTTP 409 conflicts as warnings so subsequent package pushes can continue. This is useful for rerun recovery, but Story 12.5 owns adding stronger partial-publish alerting. Source: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-push
- NuGet.org publishing requires an API key; ownership is associated with the account or key used to publish. Source: https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package

## Previous Story Intelligence

There is no previous Epic 12 story. Carry forward these Epic 11 lessons instead:

- External repository settings need first-class status. Branch protection and `NUGET_API_KEY` are not normal repo edits.
- Tolerant release defaults can hide real failures. Do not expand `--skip-duplicate`, missing-artifact tolerance, or zero-test tolerance without explicit evidence and alerting.
- Story file scope is a contract. This story is release validation and documentation, not runtime stabilization.
- The release package set is seven packages, not the older PRD wording that mentioned eight packages.
- The first release runbook must be written from observed behavior, not invented from desired behavior.
- `package-lock.json` is tracked now; do not reintroduce the Epic 11 P1 problem.

## Git Intelligence Summary

Recent local history:

- `d697f45 chore: Refresh Epic 11 retrospective and update related documentation`
- `f4b5038 chore(release): 1.2.0 [skip ci]`
- `e6d9e57 feat: Complete retrospectives for Epics 8, 9, and 10; update sprint status`
- `245d261 chore: Complete Epic 7 retrospective and update Hexalith.EventStore submodule to latest commit`
- `1b08e17 chore: Update retrospectives for Epic 5 and Epic 6, marking them as completed; update Hexalith.EventStore submodule to latest commit`

Remote branch `origin/codex/update-memories-release-path` includes commits relevant to this story, including `332e267 feat: Add release runbook and document first release path validation`. Treat this as prior art to inspect, not as automatically merged truth.

## Risks and Guardrails

- Risk: Marking the story complete while `main` is still unprotected. Guardrail: AC #2 requires before/after branch-protection evidence or `External Action Pending`.
- Risk: Treating `v1.2.0` as invalid because the story expected "first release" to be future. Guardrail: reconcile the actual release evidence and document what happened.
- Risk: Leaking `NUGET_API_KEY`. Guardrail: verify by workflow references and release success only; never print, copy, or screenshot secret values.
- Risk: Scope creep into Stories 12.2-12.6. Guardrail: keep tolerance checklist, file-scope automation, baseline sweep, partial-publish alerting, and S11-FA resolution in their own stories.
- Risk: Package inventory drift. Guardrail: use `tools/release-packages.json` and `tools/validate-release-packages.ps1` as source of truth.
- Risk: Branch protection may block the existing `@semantic-release/git` changelog commit on a future release. Guardrail: document the risk in the runbook and handle any strategy change in a separate story rather than weakening branch protection silently.
- Risk: Re-validating stale evidence as if it were current. Guardrail: every external fact must be rechecked during implementation and recorded with date/source.
- Risk: Prior branch work is copied without understanding. Guardrail: require a reuse/reject/defer summary before porting any content.
- Risk: Historical release evidence is mistaken for permission to publish again. Guardrail: no-new-release validation is mandatory unless Jerome explicitly approves a live release.
- Risk: The next release publishes packages and then fails to push the release commit because of branch protection. Guardrail: semantic-release + branch-protection compatibility must be decided or explicitly deferred before closing the story.

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 12 and Story 12.1 acceptance criteria.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-26.md` - Option C, Operations Epic 12 first.
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-30.md` - refreshed Epic 11 carry-forward findings.
- `_bmad-output/implementation-artifacts/11-2-semantic-release-and-nuget-publishing.md` - release automation, package inventory, and review findings.
- `_bmad-output/implementation-artifacts/deferred-work.md` - S11-FA and S11-FD follow-ups.
- `docs/dev/branch-protection.md` - required branch-protection settings.
- `CONTRIBUTING.md` - release and package publishing process.
- `.github/workflows/release.yml` - release job.
- `.github/workflows/ci.yml` - required CI check names.
- `.releaserc.json` - semantic-release command chain.
- `tools/release-packages.json` - approved package set.
- `tools/pack-release.ps1`, `tools/validate-release-packages.ps1`, `tools/publish-nuget.ps1`, `tools/test-release.ps1` - release script contracts.
- GitHub Release: https://github.com/Hexalith/Hexalith.Memories/releases/tag/v1.2.0
- NuGet flat-container root pattern: `https://api.nuget.org/v3-flatcontainer/{lower-package-id}/index.json`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `python3 _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-dev-story --key workflow`
- `gh repo view Hexalith/Hexalith.Memories --json nameWithOwner,defaultBranchRef,viewerPermission`
- `gh api repos/Hexalith/Hexalith.Memories --jq '{name, full_name, private, default_branch, permissions}'`
- `gh api user --jq '{login: .login}'`
- `git show --stat --oneline --decorate v1.2.0`
- `git show -s --format='%H%n%h%n%an%n%ae%n%ad%n%s%n%P' --date=iso-strict v1.2.0`
- `gh run view 25148312032 --repo Hexalith/Hexalith.Memories --json databaseId,displayTitle,headBranch,headSha,event,status,conclusion,createdAt,updatedAt,url,jobs`
- `gh release view v1.2.0 --json tagName,name,publishedAt,url,isDraft,isPrerelease,assets`
- `gh api repos/Hexalith/Hexalith.Memories/branches/main/protection`
- `gh api repos/Hexalith/Hexalith.Memories/branches/main --jq '{name: .name, protected: .protected, protection_url: .protection_url, commit: .commit.sha}'`
- `gh api repos/Hexalith/Hexalith.Memories/rulesets`
- `gh api repos/Hexalith/Hexalith.Memories/rules/branches/main`
- `gh api repos/Hexalith/Hexalith.Memories/branches/main/protection --method PUT --input -`
- `gh api repos/Hexalith/Hexalith.Memories/rules/branches/main` after maintainer ruleset activation
- `gh api repos/Hexalith/Hexalith.Memories/rulesets` after maintainer ruleset activation
- `gh api repos/Hexalith/Hexalith.Memories/branches/main/protection` after maintainer ruleset activation
- Final validation: temporary PowerShell 7 tool install outside the repo, then `pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1`
- Final validation: `Select-String ... -Pattern '- \[ \]'` found no unchecked story tasks.
- `git show origin/codex/update-memories-release-path:docs/dev/release-runbook.md`
- `git show origin/codex/update-memories-release-path:docs/dev/branch-protection.md`
- `git show --stat --oneline 332e267`
- NuGet flat-container checks for all seven approved package IDs.
- Temporary PowerShell 7 tool install outside the repo, then `pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1`
- `gh release download v1.2.0 --repo Hexalith/Hexalith.Memories --pattern '*.nupkg' --dir <temp>`
- `pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1 -PackageDirectory <temp> -Version 1.2.0`

### Completion Notes List

- Story moved to `in-progress` on 2026-04-30 and sprint status updated accordingly.
- Captured release run `25148312032`: `Release` completed successfully on 2026-04-30 05:04:45 UTC from source commit `e6d9e5764582709620a7449df5459d2680619451`.
- Captured release tag evidence: `v1.2.0` points to `f4b5038b3d495e424f266797b11d8d72b69030b1`, authored by `semantic-release-bot`, message `chore(release): 1.2.0 [skip ci]`.
- Captured GitHub Release evidence: `v1.2.0` published on 2026-04-30 05:04:38 UTC with seven `.nupkg` assets matching `tools/release-packages.json`.
- Captured NuGet evidence: all seven approved packages report versions `1.0.0`, `1.1.0`, and `1.2.0`, with latest `1.2.0`, from the flat-container API.
- Validated package metadata with the canonical script under PowerShell 7. Both inventory-only validation and downloaded `v1.2.0` package validation passed. Windows PowerShell 5.1 failed because it lacks `[System.IO.Path]::GetRelativePath`; this is an environment mismatch, not a package validation failure.
- Inspected `origin/codex/update-memories-release-path` via targeted `git show`, `git log`, and branch-file reads. Reused: release-runbook structure, observed `v1.2.0` evidence shape, package evidence layout, second-release checklist, recovery-note categories, and semantic-release/branch-protection risk framing. Rejected: prior branch changes outside this story's current file scope and any wholesale branch merge. Deferred/conflicting: prior-branch updates to `deferred-work.md` and done status claims, because this dev-story workflow moves the story to review first.
- Attempted to apply the required classic branch protection for `main` with the documented required checks, strict status checks, one approval, admin enforcement, and force-push/deletion blocking. GitHub returned HTTP 404. Current token has push/write but not maintain/admin.
- Jerome activated the `main` ruleset on 2026-04-30. Post-activation API evidence confirms ruleset `main` (`id: 15760772`) has `enforcement: active`; active rules for `main` include `deletion`, `non_fast_forward`, `pull_request`, and `required_status_checks`; required approvals are `1`; strict/up-to-date status checks are enabled for `build`, `test-unit-contract`, and `integration-fast`; classic branch protection reports `enforce_admins.enabled: true`, `allow_force_pushes.enabled: false`, and `allow_deletions.enabled: false`.
- Final validation passed after branch-protection verification: `tools/validate-release-packages.ps1` passed under PowerShell 7 and no unchecked story tasks remained.
- Added `docs/dev/release-runbook.md`, updated `docs/dev/branch-protection.md` with current evidence and active branch-protection status, and added a `CONTRIBUTING.md` cross-link.
- Documented semantic-release + branch-protection compatibility risk: `@semantic-release/git` may fail to push the `CHANGELOG.md` release commit once branch protection is enforced; handle that as follow-up release strategy rather than weakening protection silently.
- No new tags were created, no NuGet publish was run locally, no release package inventory was changed, and no release-triggering commit was merged.
- Code review close-out removed the unintended untracked `Hexalith.Memories.sln` from the story change set and updated the runbook with before/after branch-protection evidence plus the stale-approval dismissal state and rationale.

### File List

- `CONTRIBUTING.md`
- `docs/dev/branch-protection.md`
- `docs/dev/release-runbook.md`
- `_bmad-output/implementation-artifacts/12-1-first-release-path-validation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-04-30: Captured release/package evidence, validated downloaded `v1.2.0` packages, added release runbook, clarified branch-protection status, and left story in progress with `External Action Pending` because required branch protection was not yet verifiably enforced.
- 2026-04-30: Verified maintainer-activated `main` ruleset and classic branch protection evidence, updated runbook and branch-protection docs, completed remaining branch-protection tasks, and moved story to review.

## Story Completion Status

Implementation and review close-out complete. Release evidence, package validation, runbook documentation, prior-branch reconciliation, branch-protection verification, no-new-release guardrails, and review patches are complete. Story status moved to `done` on 2026-04-30.
