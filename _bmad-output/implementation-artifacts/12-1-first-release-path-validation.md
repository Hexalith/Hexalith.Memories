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

This story was originally planned before the first publish ran. Local and remote evidence now show that release automation has already produced real release artifacts, but the branch-protection prerequisite is still not enforced.

- `git tag v1.2.0` exists at commit `f4b5038b3d495e424f266797b11d8d72b69030b1`, authored by `semantic-release-bot` on 2026-04-30 05:04:19 UTC.
- GitHub Release `v1.2.0` was published on 2026-04-30 05:04:38 UTC with seven `.nupkg` assets: `Contracts`, `Client.Rest`, `Redis`, `Cli`, `Mcp`, `EventStore`, and `Telemetry`.
- NuGet flat-container API reports versions `1.0.0`, `1.1.0`, and `1.2.0` for all seven approved packages as of 2026-04-30.
- GitHub Actions release run `25148312032` completed successfully on 2026-04-30 05:04:45 UTC. Steps `Build`, `Test unit and non-Docker suite`, `Validate package inventory before release`, `Run semantic-release`, and `Upload package artifacts` all succeeded.
- `gh api repos/Hexalith/Hexalith.Memories/branches/main/protection` currently returns `404 Branch not protected`.
- `docs/dev/release-runbook.md` does not exist yet.

Implementation must therefore treat 12.1 as a release-path reconciliation and hardening story: preserve the real release evidence, apply or verify branch protection, verify NuGet/GitHub artifacts, and write the observed runbook. Do not force an artificial new release only to satisfy stale wording.

## Acceptance Criteria

1. Given Epic 11 retrospective action A1 has a documented branch-protection contract in `docs/dev/branch-protection.md`, when a maintainer applies repository protection for `main`, then GitHub enforces at least one approval, blocks direct pushes, and requires exactly these stable checks: `build`, `test-unit-contract`, and `integration-fast`.

2. Given the current GitHub API reports `main` is not protected, when this story is completed, then the story completion notes and runbook include the before/after evidence for branch protection. If the developer cannot apply the setting due to permissions, the story must remain blocked or explicitly mark `External Action Pending`; it must not be moved to `done`.

3. Given Epic 11 retrospective action A2 requires a scoped `NUGET_API_KEY` repository secret, when release validation is documented, then `release.yml` is shown to reference `secrets.NUGET_API_KEY`, no secret value is logged or copied into repo files, and the observed successful release run proves the credential was available to semantic-release/publish.

4. Given release run `25148312032` already produced `v1.2.0`, when the maintainer validates the release path, then the runbook records the observed sequence: push to `main`, `release.yml` execution, semantic-release version calculation, package pack/validation, NuGet publish, tag creation, GitHub Release creation, and `[skip ci]` release commit behavior.

5. Given the approved package inventory in `tools/release-packages.json`, when the released version is inspected, then all seven packages are present on nuget.org at the same version, have corresponding GitHub Release assets, and match the approved package set: `Hexalith.Memories.Contracts`, `Hexalith.Memories.Client.Rest`, `Hexalith.Memories.Redis`, `Hexalith.Memories.Cli`, `Hexalith.Memories.Mcp`, `Hexalith.Memories.EventStore`, and `Hexalith.Memories.Telemetry`.

6. Given package validation is part of the release contract, when the release evidence is captured, then `tools/validate-release-packages.ps1` remains the canonical verifier for package metadata, package IDs, README packing, and internal dependency version equality. Do not replace it with manual inspection.

7. Given the first release has succeeded, then `docs/dev/release-runbook.md` exists and documents the repeatable second-release path using observed evidence, not speculative instructions.

8. Given Epic 12 deliberately defers Phase 2 feature work, then this story does not modify runtime source code, public API contracts, MCP behavior, EventStore integration, or package inventory unless a release-path defect is found and explicitly documented with a `Scope-Override:` rationale or a follow-up story.

## Tasks / Subtasks

- [x] Task 1 - Capture immutable release evidence (AC: 3, 4, 5)
  - [x] Record GitHub Actions release run URL and run ID `25148312032`.
  - [x] Record the `v1.2.0` tag, release commit `f4b5038`, release commit message `chore(release): 1.2.0 [skip ci]`, and source commit `e6d9e57`.
  - [x] Record the GitHub Release URL and seven asset names.
  - [x] Query the NuGet flat-container API for each approved package and record the latest version observed.

- [x] Task 2 - Apply or verify branch protection (AC: 1, 2)
  - [x] Follow `docs/dev/branch-protection.md` for `main`.
  - [x] Require pull request before merge.
  - [x] Require at least one approval.
  - [x] Require status checks `build`, `test-unit-contract`, and `integration-fast`.
  - [x] Block direct pushes to `main`.
  - [x] Re-run `gh api repos/Hexalith/Hexalith.Memories/branches/main/protection` and capture the resulting settings.
  - [x] If permissions are insufficient, update the story completion notes with `External Action Pending` and do not claim AC #1/#2 complete. Not applicable; permissions were sufficient and protection is enforced.

- [x] Task 3 - Verify release workflow and secret usage (AC: 3, 4)
  - [x] Confirm `.github/workflows/release.yml` still runs only on pushes to `main`.
  - [x] Confirm semantic-release receives `GITHUB_TOKEN` and `NUGET_API_KEY` through environment variables only.
  - [x] Confirm `.releaserc.json` still invokes `tools/pack-release.ps1` in `prepareCmd` and `tools/publish-nuget.ps1` in `publishCmd`.
  - [x] Confirm the release commit skip behavior is intentional and documented in the runbook.

- [x] Task 4 - Validate package inventory and published artifacts (AC: 5, 6)
  - [x] Run `./tools/validate-release-packages.ps1`.
  - [x] If release packages are available locally, run `./tools/validate-release-packages.ps1 -PackageDirectory <dir> -Version 1.2.0`.
  - [x] Verify GitHub Release assets match `tools/release-packages.json`.
  - [x] Verify all approved packages are present on nuget.org at the released version.
  - [x] Do not add or remove packages from `tools/release-packages.json` in this story.

- [x] Task 5 - Write the observed release runbook (AC: 4, 7)
  - [x] Add `docs/dev/release-runbook.md`.
  - [x] Include prerequisites: branch protection, scoped `NUGET_API_KEY`, clean `main`, conventional commit, package inventory validation.
  - [x] Include the observed first-release evidence from `v1.2.0`.
  - [x] Include the second-release checklist.
  - [x] Include recovery notes for failed release runs and partial publish, but keep implementation of partial-publish alerting in Story 12.5.

- [x] Task 6 - Close status honestly (AC: 1-8)
  - [x] Update this story's Dev Agent Record with commands run and links/evidence captured.
  - [x] If branch protection is still not enforced, leave the story blocked or in progress with `External Action Pending`. Not applicable; protection is enforced.
  - [x] If all ACs are satisfied, update sprint status from `ready-for-dev` to the next workflow state according to the dev-story/code-review workflow, not manually to `done`.

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
- Runtime behavior changes
- MCP, EventStore, Redis, Server, CLI, or Contracts changes

If a forbidden file must change to fix a real release-path defect, stop and either create a follow-up story or add an explicit `Scope-Override:` rationale in the commit/story notes.

## Dev Notes

### Epic Context

Epic 12 exists to prove operations and release readiness before any Phase 2 feature investment. It is driven by the Epic 11 retrospective and Sprint Change Proposal 2026-04-26 Option C. Do not broaden this story into deferred Phase 2 items such as per-tenant LLM configuration, tokenizer-accurate budgets, projection registry, MCP trace-hop proof, or EventStore feature work.

Story 12.1 closes the gap between "release automation exists" and "release path is proven and governable." The first actual release appears to have already occurred, so the main risks are incomplete governance and missing documentation: branch protection is absent, the runbook is missing, and sprint status still says this story is backlog until this file is created.

### Current Release Surface

Current files and behaviors to preserve:

- `.github/workflows/release.yml` restores, builds, runs `tools/test-release.ps1`, installs npm tooling, validates package inventory, runs `npx semantic-release`, and uploads release package artifacts.
- `.releaserc.json` uses `@semantic-release/commit-analyzer`, release notes, exec, and GitHub plugins. The exec plugin runs `pack-release.ps1` during prepare and `publish-nuget.ps1` during publish. It does not use changelog/git commit plugins, so release jobs do not push commits back to protected `main`.
- `tools/release-packages.json` is the approved package inventory and currently lists seven published packages plus three non-packable projects.
- `tools/pack-release.ps1` validates inventory, deletes/recreates the output directory, builds the solution once with the release version, packs each approved package with `--no-build`, and validates generated packages.
- `tools/validate-release-packages.ps1` enforces packable/non-packable inventory, required NuGet metadata, packed README presence, case-sensitive package IDs, generated package inventory, released version, and exact internal dependency version lower bounds.
- `tools/publish-nuget.ps1` requires `NUGET_API_KEY`, validates generated packages before publishing, pushes sorted `.nupkg` files to `https://api.nuget.org/v3/index.json`, and uses `--skip-duplicate`.
- `tools/test-release.ps1` intentionally filters `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` as S11-FA. Do not solve S11-FA here; Story 12.6 owns it.

### External Actions and Permissions

Branch protection and repository secrets are maintainer-owned external actions. The developer may be able to inspect GitHub settings with `gh`, but must not claim completion unless the settings are enforced. Current observed state on 2026-04-30:

- `gh api repos/Hexalith/Hexalith.Memories/branches/main/protection` returns `404 Branch not protected`.
- `gh api repos/Hexalith/Hexalith.Memories/rulesets` returns no active ruleset entries.
- The successful `v1.2.0` release proves a NuGet credential was available during release run `25148312032`, but GitHub secrets cannot be read back by value. Document presence through workflow behavior and settings screenshot/API metadata only; never expose secret values.

### Release Evidence to Capture

Use these commands as starting points:

```powershell
git show --stat --oneline --decorate v1.2.0
gh run view 25148312032 --json name,displayTitle,conclusion,status,createdAt,updatedAt,event,headSha,jobs,url
gh release view v1.2.0 --json tagName,name,publishedAt,url,isDraft,isPrerelease,assets
gh api repos/Hexalith/Hexalith.Memories/branches/main/protection
./tools/validate-release-packages.ps1
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
    [pscustomobject]@{ Package = $id; Latest = ($versions | Select-Object -Last 1) }
}
```

### Testing Requirements

Minimum validation before completing this story:

- `./tools/validate-release-packages.ps1`
- GitHub API verification for branch protection or explicit `External Action Pending`
- GitHub Release asset inventory check for `v1.2.0`
- NuGet flat-container version check for all seven packages
- Markdown review of `docs/dev/release-runbook.md`

Do not run a real `dotnet nuget push` locally. Publishing is CI-only per `CONTRIBUTING.md`.

### Latest Technical Information

Web verification performed on 2026-04-30:

- GitHub branch protection supports requiring pull requests, approval counts, required status checks, and push restrictions for protected branches. The project-specific required checks remain `build`, `test-unit-contract`, and `integration-fast`. Source: https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/managing-a-branch-protection-rule
- GitHub Actions recognizes `[skip ci]` and `[skip actions]` in commit messages for `push` and `pull_request` workflows. This matches the release commit guard in `release.yml` and the semantic-release commit message. Source: https://docs.github.com/en/actions/how-tos/manage-workflow-runs/skip-workflow-runs
- semantic-release requires remote push access to create git tags and can use `GITHUB_TOKEN`/`GH_TOKEN` for GitHub authentication. The NuGet credential remains project-specific through `NUGET_API_KEY`. Source: https://semantic-release.gitbook.io/semantic-release/usage/ci-configuration
- `dotnet nuget push --skip-duplicate` treats HTTP 409 conflicts as warnings so subsequent package pushes can continue. This is useful for rerun recovery, but Story 12.5 owns adding a stronger partial-publish alert. Source: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-push

## Previous Story Intelligence

There is no previous Epic 12 story. Carry forward these Epic 11 lessons instead:

- External repository settings need first-class status. Branch protection and `NUGET_API_KEY` are not normal repo edits.
- Tolerant release defaults can hide real failures. Do not expand `--skip-duplicate`, missing-artifact tolerance, or zero-test tolerance without explicit evidence and alerting.
- Story file scope is a contract. This story is release validation and documentation, not runtime stabilization.
- The release package set is seven packages, not the older PRD wording that mentioned eight packages.
- The first release runbook must be written from observed behavior, not invented from desired behavior.

## Risks and Guardrails

- Risk: Marking the story complete while `main` is still unprotected. Guardrail: AC #2 requires before/after branch-protection evidence or `External Action Pending`.
- Risk: Treating `v1.2.0` as invalid because the story expected "first release" to be future. Guardrail: reconcile the actual release evidence and document what happened.
- Risk: Leaking `NUGET_API_KEY`. Guardrail: verify by workflow references and release success only; never print, copy, or screenshot secret values.
- Risk: Scope creep into Stories 12.2-12.6. Guardrail: keep tolerance checklist, file-scope automation, baseline sweep, partial-publish alerting, and S11-FA resolution in their own stories.
- Risk: Package inventory drift. Guardrail: use `tools/release-packages.json` and `tools/validate-release-packages.ps1` as source of truth.

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 12 and Story 12.1 acceptance criteria.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-26.md` - Option C, Operations Epic 12 first.
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-30.md` - refreshed Epic 11 carry-forward findings.
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md` - original Epic 11 action items A1-A5 and S11-FA/S11-FD.
- `docs/dev/branch-protection.md` - required branch-protection settings.
- `CONTRIBUTING.md` - release and package publishing process.
- `.github/workflows/release.yml` - release job.
- `.github/workflows/ci.yml` - required CI check names.
- `.releaserc.json` - semantic-release command chain.
- `tools/release-packages.json` - approved package set.
- `tools/pack-release.ps1`, `tools/validate-release-packages.ps1`, `tools/publish-nuget.ps1`, `tools/test-release.ps1` - release script contracts.
- GitHub Release: https://github.com/Hexalith/Hexalith.Memories/releases/tag/v1.2.0
- GitHub Actions release run: https://github.com/Hexalith/Hexalith.Memories/actions/runs/25148312032
- NuGet flat-container root pattern: `https://api.nuget.org/v3-flatcontainer/{lower-package-id}/index.json`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `git show --stat --oneline --decorate v1.2.0`
- `git show -s --format='%H%n%h%n%an%n%ae%n%ad%n%s' --date=iso-strict v1.2.0`
- `gh run view 25148312032 --json name,displayTitle,conclusion,status,createdAt,updatedAt,event,headSha,jobs,url`
- `gh release view v1.2.0 --json tagName,name,publishedAt,url,isDraft,isPrerelease,assets`
- `gh api repos/Hexalith/Hexalith.Memories/branches/main/protection`
- `gh api repos/Hexalith/Hexalith.Memories/rulesets`
- `gh api repos/Hexalith/Hexalith.Memories/rulesets/15760772`
- `gh api repos/Hexalith/Hexalith.Memories/branches/main`
- `./tools/validate-release-packages.ps1`
- `gh release download v1.2.0 --pattern '*.nupkg' --dir <temp>`
- `./tools/validate-release-packages.ps1 -PackageDirectory <temp> -Version 1.2.0`
- NuGet flat-container API checks for all seven approved packages.

### Completion Notes List

- Story context created on 2026-04-30.
- Initial story evidence showed `v1.2.0` had already been published, but `main` branch protection was not enforced.
- Captured release run `25148312032`: `Release` completed successfully on 2026-04-30 05:04:45 UTC from source commit `e6d9e5764582709620a7449df5459d2680619451`.
- Captured release tag evidence: `v1.2.0` points to `f4b5038b3d495e424f266797b11d8d72b69030b1`, authored by `semantic-release-bot`, message `chore(release): 1.2.0 [skip ci]`.
- Captured GitHub Release evidence: `v1.2.0` published on 2026-04-30 05:04:38 UTC with seven `.nupkg` assets matching `tools/release-packages.json`.
- Captured NuGet evidence: all seven approved packages report latest version `1.2.0` from the flat-container API.
- Applied classic branch protection for `main` on 2026-04-30. Before evidence was `404 Branch not protected`; after evidence reports `protected: true`, strict required checks `build`, `test-unit-contract`, `integration-fast`, one required approval, `enforce_admins.enabled: true`, force pushes disabled, and deletions disabled.
- Verified `.github/workflows/release.yml` runs on pushes to `main`, passes `GITHUB_TOKEN` and `NUGET_API_KEY` through environment variables only, and does not expose secret values.
- Verified `.releaserc.json` still invokes `tools/pack-release.ps1` in `prepareCmd` and `tools/publish-nuget.ps1` in `publishCmd`.
- Ran `./tools/validate-release-packages.ps1`: passed.
- Downloaded GitHub Release `v1.2.0` `.nupkg` assets to a temp directory and ran `./tools/validate-release-packages.ps1 -PackageDirectory <temp> -Version 1.2.0`: passed.
- Added observed release runbook at `docs/dev/release-runbook.md`, including prerequisites, first-release evidence, second-release checklist, branch-protection evidence, package evidence, secret-handling guardrails, and partial-publish recovery notes.
- Follow-up release hardening removed the `@semantic-release/git` changelog commit path after PR #12's post-merge release run confirmed protected `main` rejects direct release-commit pushes.

### File List

- `docs/dev/release-runbook.md`
- `docs/dev/branch-protection.md`
- `_bmad-output/implementation-artifacts/12-1-first-release-path-validation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-04-30: Applied `main` branch protection, captured release/package evidence, added release runbook, and moved Story 12.1 to review.
- 2026-04-30: Code-review patch pass — 15 patches applied across `docs/dev/release-runbook.md` and `docs/dev/branch-protection.md`. Added `strict: true` and `enforce_admins.enabled: true` to the documented Required Main Settings (the API surface for "Require branches up to date before merging" and "Include administrators"); added a required-check → `ci.yml` job-ID mapping table; clarified that PR-merge events drive the release `push` trigger; clarified that v1.2.0 was published before branch protection was applied; corrected the observed-release sequence to reflect that `pack-release.ps1`/`publish-nuget.ps1` run inside semantic-release plugins, not as separate workflow steps; replaced the paraphrased 404 quote with the literal `gh api` body; added an explicit source-vs-tag-commit relationship; added `clean main` prerequisite; reconciled "no active rulesets" wording with "ruleset exists, enforcement disabled"; distinguished HTTP-409 from non-409 publish failure recovery; pointed operators at the GitHub Actions workflow log as the source of truth for partial-publish state; and cross-referenced S11-FA / Story 12.6 from recovery notes. 6 deferred items recorded in `deferred-work.md`. Status moved review → done.

## Story Completion Status

Implementation complete and code-reviewed. All 8 acceptance criteria satisfied (Acceptance Auditor: 8/8 PASS). Adversarial review layers found 0 critical / 0 high / 0 medium issues against the implementation; 15 documentation-precision patches were applied during the review pass and all are checked off above. 5 follow-ups deferred to `deferred-work.md` (12.1-RV1 through 12.1-RV5; 12.1-RV6 was promoted to a patch). Status moved review → done on 2026-04-30.

### Review Findings

Code review completed 2026-04-30 via parallel adversarial layers (Blind Hunter, Edge Case Hunter, Acceptance Auditor). AC coverage: 8/8 PASS. No `decision-needed` items. No critical or high-severity findings against the implementation; the high-severity items raised by the Blind Hunter were context-blindness artifacts (Blind Hunter has no access to the spec or the project, so it flagged unverifiable claims that the Edge Case Hunter and Acceptance Auditor confirmed are correct against `.github/workflows/release.yml`, `.releaserc.json`, `tools/release-packages.json`, and `tools/publish-nuget.ps1`).

#### Patches

- [x] [Review][Patch] Stale "AC #6 pending maintainer action" wording contradicts new "Applied Evidence" section [docs/dev/branch-protection.md:29-30]
- [x] [Review][Patch] NuGet table column "latest on 2026-04-30" vs prose "1.0.0, 1.1.0, 1.2.0" — clarify whether 1.2.0 is the latest or whether 1.0.0/1.1.0 are observed historic versions [docs/dev/release-runbook.md:104-114]
- [x] [Review][Patch] Add an explicit sentence stating v1.2.0 was published BEFORE branch protection was applied; the warning about `@semantic-release/git` applies to the next release [docs/dev/release-runbook.md:94-98]
- [x] [Review][Patch] Sequence accuracy: `tools/pack-release.ps1` is invoked BY `npx semantic-release` as `prepareCmd`, not a step running after semantic-release; same for `tools/publish-nuget.ps1` and `publishCmd` [docs/dev/release-runbook.md:73-86]
- [x] [Review][Patch] Reconcile "Releases are produced on pushes to `main`" with "Direct pushes are blocked" — clarify push events are PR-merge commits [docs/dev/release-runbook.md:8 vs :25]
- [x] [Review][Patch] Add explicit relationship: `v1.2.0` tag points at release commit `f4b5038` (child of source commit `e6d9e57`) [docs/dev/release-runbook.md:63-64]
- [x] [Review][Patch] Cross-link the three required check names (`build`, `test-unit-contract`, `integration-fast`) to the `.github/workflows/ci.yml` job IDs that produce them so renames don't silently break protection [docs/dev/release-runbook.md:24, docs/dev/branch-protection.md:13-16]
- [x] [Review][Patch] Add `Require branches to be up to date before merging` (the API surface for `required_status_checks.strict: true`) to "Required Main Settings" — currently applied but undocumented in the contract [docs/dev/branch-protection.md:7-18]
- [x] [Review][Patch] Add `Include administrators in branch protection` (`enforce_admins.enabled: true`) to "Required Main Settings" — currently applied but undocumented in the contract [docs/dev/branch-protection.md:7-18]
- [x] [Review][Patch] Recovery note should state the GitHub Actions workflow log is the source of truth for which packages were published before a partial failure (until Story 12.5 ships alerting) [docs/dev/release-runbook.md:153-155]
- [x] [Review][Patch] Distinguish HTTP 409 (`--skip-duplicate` skips silently) vs non-409 (`publish-nuget.ps1` throws and the foreach loop ends, so alphabetically-later packages are never attempted) in recovery notes [docs/dev/release-runbook.md:153-155]
- [x] [Review][Patch] Replace paraphrased `HTTP 404: Branch not protected` text-block with the literal API JSON body for fidelity [docs/dev/release-runbook.md:35-38]
- [x] [Review][Patch] Reconcile story file wording "rulesets returned no active ruleset entries" vs runbook wording "ruleset named `main` also existed, but its enforcement was `disabled`" — clarify "active" means "enforced" [_bmad-output/implementation-artifacts/12-1-first-release-path-validation.md:147 vs docs/dev/release-runbook.md:55-57]
- [x] [Review][Patch] Add explicit "clean `main`" / "no uncommitted local edits" to runbook prerequisites — listed in the spec's Task 5 sub-bullet but missing from runbook [docs/dev/release-runbook.md:17-29]
- [x] [Review][Patch] Branch-protection.md "Applied Evidence" lists fields that imply but do not directly assert direct-push blocking — add a one-line note that required PR review + `enforce_admins` together block direct pushes for non-admins/admins respectively [docs/dev/branch-protection.md:39-48]

#### Deferred

- [x] [Review][Defer] Add SHA-256 / checksum evidence to package-evidence table — out of Story 12.1 scope, candidate for Story 12.5 or a follow-up audit story
- [x] [Review][Defer] Pin "semantic-release-bot" display name to a concrete GitHub App / user identity for forensic clarity — minor follow-up, no story claim depends on this
- [x] [Review][Defer] Document edge case where a PR-merge commit message containing the substring `[skip ci]` would silently skip the release workflow — speculative edge case
- [x] [Review][Defer] Verify `package-lock.json` is tracked in git — already on the Epic 11 follow-up list (sprint-status comment 2026-04-26 P1)
- [x] [Review][Defer] Add `CONTRIBUTING.md` cross-link to the new release runbook — File Scope permits but does not require this; small discoverability win
- [x] [Review][Defer→Patched] Cross-reference Story 12.6 (S11-FA `EmbeddingInputContentKindTests` baseline filter) from runbook recovery notes — promoted from defer to patch during the patch pass because it is a single line of operator-context inside an already-edited section; resolution of S11-FA itself remains owned by Story 12.6

#### Dismissed (9)

Blind Hunter raised 9 findings invalidated by project context: future-dated-evidence concern (today is 2026-04-30, claims are real), GitHub Actions run ID format speculation (real run `25148312032` exists), step-description duplication between high-level contract and observed-run sequence (different abstraction layers, both accurate), nested-code-fence rendering risk (actual file is well-formed), Second Release Checklist ordering bug (items are sequential pre-merge → merge → post-merge), Story 12.5 forward reference unverifiable (defined in sprint-status.yaml lines 196-197), YAML-comment process instruction unenforceable (BMad workflow artifact, not enforced code), `enforce_admins` speculation about release-bot identity (applied correctly; semantic-release uses `GITHUB_TOKEN`), and three-way repetition of validator-canonicality assertion (defensive repetition by design).
