# Release Runbook

This runbook records the observed first release path (`v1.2.0`, 2026-04-30) and the repeatable
path for the next release. Package publishing remains CI-only. Do not run `dotnet nuget push` from
a workstation.

## Current Release Contract

- Releases are produced by `.github/workflows/release.yml` on `push` events targeting `main`. With
  branch protection now enabled, those `push` events come from PR-merge commits, not direct pushes
  from a workstation.
- The release job restores, builds, runs `tools/test-release.ps1`, installs npm tooling, validates
  package inventory, and runs `npx semantic-release`. Semantic-release then drives the prepare,
  publish, and post-publish phases through its plugin chain — `pack-release.ps1` and
  `publish-nuget.ps1` are not invoked as separate workflow steps; they are invoked by
  `@semantic-release/exec` during the corresponding semantic-release lifecycle phase.
- Semantic-release uses `.releaserc.json`.
- `tools/pack-release.ps1` is the `prepareCmd` for `@semantic-release/exec`.
- `tools/publish-nuget.ps1` is the `publishCmd` for `@semantic-release/exec`.
- `tools/validate-release-packages.ps1` is the canonical package verifier (used both before
  semantic-release runs and inside `pack-release.ps1` after pack).
- `tools/release-packages.json` is the approved package inventory.
- `NUGET_API_KEY` is provided only through `secrets.NUGET_API_KEY` in GitHub Actions and is forwarded
  to the release job as an environment variable; it is never written to a file in the repo.

The required-check names mapping to `.github/workflows/ci.yml` job IDs is documented in
[`branch-protection.md`](./branch-protection.md).

## Prerequisites

Before relying on the release path:

1. `main` branch protection is enabled (see [`branch-protection.md`](./branch-protection.md)).
2. Pull requests are required before merge.
3. At least one approving review is required.
4. Required checks are exactly `build`, `test-unit-contract`, and `integration-fast`.
5. Direct pushes are blocked by the protected-branch policy; the only path to producing a `push`
   event on `main` is merging a PR.
6. A scoped `NUGET_API_KEY` repository secret exists.
7. The merge to `main` uses a conventional commit that semantic-release can classify.
8. The local working tree is clean before opening the release PR — no uncommitted edits, no
   in-flight tag drift, no orphan packages in `artifacts/`. The release job builds from the merged
   commit; clean local state is what makes the merged commit predictable.
9. `./tools/validate-release-packages.ps1` passes before release.
10. `tools/release-packages.json` still lists the intended package set.

## Branch Protection Evidence

Before Story 12.1 execution on 2026-04-30, classic branch protection was not configured for `main`.
`gh api` returned HTTP 404 with the standard "branch not protected" body:

```text
$ gh api repos/Hexalith/Hexalith.Memories/branches/main/protection
gh: Branch not protected (HTTP 404)
{
  "message": "Branch not protected",
  "documentation_url": "https://docs.github.com/rest/branches/branch-protection#get-branch-protection",
  "status": "404"
}
```

After Story 12.1 execution on 2026-04-30, the relevant fields of the protection response were:

```text
$ gh api repos/Hexalith/Hexalith.Memories/branches/main/protection
required_status_checks.strict: true
required_status_checks.contexts: build, test-unit-contract, integration-fast
required_pull_request_reviews.required_approving_review_count: 1
enforce_admins.enabled: true
allow_force_pushes.enabled: false
allow_deletions.enabled: false

$ gh api repos/Hexalith/Hexalith.Memories/branches/main
protected: true
```

A repository ruleset named `main` also exists in the repository configuration, but its enforcement
is `disabled` and it only contains deletion and non-fast-forward rules. (`gh api ... /rulesets`
returns no rulesets in `enforcement: active` state — the `main` ruleset is in `disabled`
enforcement state and therefore not active.) The enforced control on `main` for Story 12.1 is the
classic branch protection rule above; the disabled ruleset is left in place but unenforced.

## Observed First Release

Release `v1.2.0` proves the Epic 11 release automation ran end to end. Important sequencing note:
`v1.2.0` was published on 2026-04-30 **before** branch protection was applied later the same day.
The release succeeded against an unprotected `main`. The branch-protection-versus-release-commit
caveat at the end of this section therefore applies to the **next** release, not to `v1.2.0`.

- Source commit (last commit on `main` before the release run): `e6d9e5764582709620a7449df5459d2680619451`
- Release tag commit (the release commit semantic-release created and tagged): `f4b5038b3d495e424f266797b11d8d72b69030b1`
- Tag–commit relationship: `v1.2.0` points at `f4b5038`, whose first parent is the source commit
  `e6d9e57`. The release commit was created by semantic-release after publishing and before
  pushing the tag.
- Release commit message: `chore(release): 1.2.0 [skip ci]`
- Release commit author: `semantic-release-bot` (display name; the commit was authored by the
  semantic-release process running under `GITHUB_TOKEN` in the Actions run below)
- Release commit date: 2026-04-30 05:04:19 UTC
- GitHub Actions run: https://github.com/Hexalith/Hexalith.Memories/actions/runs/25148312032
- GitHub Release: https://github.com/Hexalith/Hexalith.Memories/releases/tag/v1.2.0
- GitHub Release published: 2026-04-30 05:04:38 UTC
- Release job completed: 2026-04-30 05:04:45 UTC

Observed release-job behaviour on 2026-04-30. The first five items below are workflow-level steps
in `.github/workflows/release.yml`. From `npx semantic-release` onward, the work is driven by
semantic-release plugins, not by additional workflow steps.

1. A `push` event on `main` triggered `.github/workflows/release.yml`.
2. The release job checked out the repository (submodules enabled at root level only;
   `submodules: true`, not `recursive`).
3. The job restored and built `Hexalith.Memories.slnx`.
4. `tools/test-release.ps1` ran the unit and non-Docker release test suite.
5. `tools/validate-release-packages.ps1` validated package inventory metadata before release.
6. `npx semantic-release` ran. Inside that single workflow step:
   - `@semantic-release/commit-analyzer` and `@semantic-release/release-notes-generator`
     calculated version `1.2.0`.
   - `@semantic-release/changelog` updated `CHANGELOG.md` in the working tree.
   - `@semantic-release/exec` invoked `tools/pack-release.ps1` as `prepareCmd` to pack the approved
     package set with the new version, then invoked `tools/publish-nuget.ps1` as `publishCmd` to
     push the resulting packages to nuget.org using `NUGET_API_KEY` from Actions secrets.
   - `@semantic-release/github` created the `v1.2.0` GitHub Release and uploaded the seven `.nupkg`
     assets.
   - `@semantic-release/git` created the release commit `chore(release): 1.2.0 [skip ci]` on `main`
     with the updated `CHANGELOG.md`, then pushed the new tag `v1.2.0`.
7. The `[skip ci]` token in the release commit message stops `release.yml` from re-triggering on
   its own commit; the same token also stops `ci.yml` from running on the release commit.

The release workflow guards against re-entry with:

```yaml
if: "!contains(github.event.head_commit.message, '[skip ci]') && !contains(github.event.head_commit.message, '[skip actions]')"
```

Because branch protection on `main` now blocks direct pushes and `.releaserc.json` still uses
`@semantic-release/git` to commit `CHANGELOG.md` back to `main`, the next release run could fail at
the `git` plugin even though packages would already be published. Maintainers should watch the
next release run closely. If branch protection blocks the release commit, keep package publication
intact, record the failure evidence, and handle any release-commit strategy change in a separate
story instead of weakening this story's protection contract.

## Package Evidence

The approved package set published as part of `v1.2.0` is:

| Package | NuGet latest version (2026-04-30) | GitHub Release asset |
| --- | --- | --- |
| `Hexalith.Memories.Contracts` | `1.2.0` | `Hexalith.Memories.Contracts.1.2.0.nupkg` |
| `Hexalith.Memories.Client.Rest` | `1.2.0` | `Hexalith.Memories.Client.Rest.1.2.0.nupkg` |
| `Hexalith.Memories.Redis` | `1.2.0` | `Hexalith.Memories.Redis.1.2.0.nupkg` |
| `Hexalith.Memories.Cli` | `1.2.0` | `Hexalith.Memories.Cli.1.2.0.nupkg` |
| `Hexalith.Memories.Mcp` | `1.2.0` | `Hexalith.Memories.Mcp.1.2.0.nupkg` |
| `Hexalith.Memories.EventStore` | `1.2.0` | `Hexalith.Memories.EventStore.1.2.0.nupkg` |
| `Hexalith.Memories.Telemetry` | `1.2.0` | `Hexalith.Memories.Telemetry.1.2.0.nupkg` |

The NuGet flat-container API also reports earlier versions `1.0.0` and `1.1.0` for all seven
packages from prior CI publishes; `1.2.0` is the latest version observed on 2026-04-30.

Validation commands used during Story 12.1:

```powershell
./tools/validate-release-packages.ps1
./tools/validate-release-packages.ps1 -PackageDirectory <downloaded-v1.2.0-assets> -Version 1.2.0
```

Both validations passed. Keep this script canonical; do not replace it with manual package
inspection.

## Second Release Checklist

1. Merge only through a pull request targeting `main`.
2. Confirm the PR has at least one approval.
3. Confirm required checks `build`, `test-unit-contract`, and `integration-fast` passed.
4. Confirm the merge commit follows conventional-commit semantics.
5. Confirm `tools/release-packages.json` has not drifted.
6. Run `./tools/validate-release-packages.ps1` locally before merging if package files changed.
7. Merge to `main`.
8. Watch the `Release` workflow run.
9. Confirm `Build`, `Test unit and non-Docker suite`, `Validate package inventory before release`,
   `Run semantic-release`, and `Upload package artifacts` all succeed.
10. Confirm the new tag exists.
11. Confirm the GitHub Release contains exactly the approved package assets for the new version.
12. Confirm NuGet flat-container APIs show the new version for all approved packages.
13. Confirm the release commit is either created with `[skip ci]` or any branch-protection conflict
    is captured for a follow-up story.

## Failure And Recovery Notes

- If restore, build, or test fails, fix through a normal pull request and rerun release by merging
  a new conventional commit to `main`.
- If package validation fails, treat `tools/release-packages.json` and
  `tools/validate-release-packages.ps1` as the source of truth. Do not manually publish around the
  validator.
- If semantic-release fails before publishing, inspect the run logs and fix the repository state or
  release configuration through a pull request.
- If NuGet publishing partially succeeds, do not delete published packages from nuget.org. There
  are two failure shapes to keep separate:
  - **HTTP 409 (already published).** `tools/publish-nuget.ps1` calls `dotnet nuget push
    --skip-duplicate`, which downgrades 409 to a warning and continues with the next package. A
    rerun therefore skips already-published packages without errors.
  - **Non-409 failures (auth, network, server error).** `dotnet nuget push` exits non-zero and
    `publish-nuget.ps1` throws on the first such failure. Because the script iterates over
    packages in a single `foreach` loop, alphabetically-later packages are *not* attempted on the
    failing run. A rerun re-attempts every package: 409s on already-published ones are skipped,
    and the originally-failed and never-attempted packages are pushed.
- The GitHub Actions workflow log is the source of truth for "which packages were pushed before
  the failure". Each package emits a `Publishing <PackageId>.<version>.nupkg` line in
  `publish-nuget.ps1` before the push, so the run log records the exact stop point. Stronger
  alerting on partial publish is intentionally deferred to Story 12.5; until then, read the run
  log before rerunning.
- If `tools/test-release.ps1` reports an unexpected test failure, check whether the failure name
  is on the project's tracked-baseline list before treating it as a release blocker. The script
  filters `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag`
  (in `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/`) per S11-FA; the resolution of S11-FA
  is owned by Story 12.6, not by this runbook.
- Never print, copy, or store the `NUGET_API_KEY` value in logs, docs, screenshots, or repo files.
