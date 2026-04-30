# Release Runbook

This runbook records the observed `v1.2.0` release path from 2026-04-30 and the repeatable path for
the next release. Package publishing remains CI-only. Do not run `dotnet nuget push` from a
workstation.

## Verified Observations

### Release Contract

- `.github/workflows/release.yml` runs on `push` events to `main`.
- The release job restores, builds, runs `tools/test-release.ps1`, installs npm tooling, validates
  package inventory, runs `npx semantic-release`, and uploads generated package artifacts.
- `.releaserc.json` scopes semantic-release to `main`, uses tags named `v${version}`, updates
  `CHANGELOG.md`, packs packages through `tools/pack-release.ps1`, publishes packages through
  `tools/publish-nuget.ps1`, creates the GitHub Release, uploads `.nupkg` assets, and creates the
  release commit message `chore(release): ${nextRelease.version} [skip ci]`.
- `tools/validate-release-packages.ps1` is the canonical verifier for package inventory, NuGet
  metadata, README packing, package IDs, generated package set, released version, and internal
  dependency version equality.
- `tools/release-packages.json` is the approved package inventory.
- `NUGET_API_KEY` is supplied only as `secrets.NUGET_API_KEY` in GitHub Actions. The successful
  `v1.2.0` publish proves a credential was available to the release job, but the secret value is not
  readable and must never be logged or copied into repo files.

### Observed First Release

Release `v1.2.0` proves the Epic 11 release automation ran end to end before this runbook existed on
current `main`.

- Source commit: `e6d9e5764582709620a7449df5459d2680619451`
- Release commit/tag target: `f4b5038b3d495e424f266797b11d8d72b69030b1`
- Release commit message: `chore(release): 1.2.0 [skip ci]`
- Release commit author: `semantic-release-bot <semantic-release-bot@martynus.net>`
- Release commit date: `2026-04-30T05:04:19+00:00`
- GitHub Actions run: <https://github.com/Hexalith/Hexalith.Memories/actions/runs/25148312032>
- Release job: <https://github.com/Hexalith/Hexalith.Memories/actions/runs/25148312032/job/73712820911>
- GitHub Release: <https://github.com/Hexalith/Hexalith.Memories/releases/tag/v1.2.0>
- GitHub Release published: `2026-04-30T05:04:38Z`
- Release workflow completed: `2026-04-30T05:04:45Z`

Observed run sequence from the successful Actions run:

1. A `push` event on `main` triggered `.github/workflows/release.yml`.
2. The workflow checked out the repository with submodules enabled.
3. The workflow restored and built `Hexalith.Memories.slnx`.
4. `tools/test-release.ps1` ran the unit and non-Docker release test suite.
5. `tools/validate-release-packages.ps1` validated package inventory before release.
6. `npx semantic-release` calculated version `1.2.0`, updated `CHANGELOG.md`, packed packages,
   published packages to NuGet using `NUGET_API_KEY`, created the GitHub Release, uploaded seven
   package assets, created the release commit, and pushed tag `v1.2.0`.
7. The `[skip ci]` release commit avoided recursive release or CI execution.

### Package Evidence

The approved package set for `v1.2.0` is exactly seven packages:

| Package | NuGet versions observed on 2026-04-30 | Latest | GitHub Release asset |
| --- | --- | --- | --- |
| `Hexalith.Memories.Contracts` | `1.0.0`, `1.1.0`, `1.2.0` | `1.2.0` | `Hexalith.Memories.Contracts.1.2.0.nupkg` |
| `Hexalith.Memories.Client.Rest` | `1.0.0`, `1.1.0`, `1.2.0` | `1.2.0` | `Hexalith.Memories.Client.Rest.1.2.0.nupkg` |
| `Hexalith.Memories.Redis` | `1.0.0`, `1.1.0`, `1.2.0` | `1.2.0` | `Hexalith.Memories.Redis.1.2.0.nupkg` |
| `Hexalith.Memories.Cli` | `1.0.0`, `1.1.0`, `1.2.0` | `1.2.0` | `Hexalith.Memories.Cli.1.2.0.nupkg` |
| `Hexalith.Memories.Mcp` | `1.0.0`, `1.1.0`, `1.2.0` | `1.2.0` | `Hexalith.Memories.Mcp.1.2.0.nupkg` |
| `Hexalith.Memories.EventStore` | `1.0.0`, `1.1.0`, `1.2.0` | `1.2.0` | `Hexalith.Memories.EventStore.1.2.0.nupkg` |
| `Hexalith.Memories.Telemetry` | `1.0.0`, `1.1.0`, `1.2.0` | `1.2.0` | `Hexalith.Memories.Telemetry.1.2.0.nupkg` |

Validation performed during Story 12.1:

```powershell
# PowerShell 7 was installed into a temporary tool directory because this workstation did not have pwsh on PATH.
pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1
pwsh -NoLogo -NoProfile -File ./tools/validate-release-packages.ps1 -PackageDirectory <temp-v1.2.0-assets> -Version 1.2.0
```

Both validations passed. Windows PowerShell 5.1 is not a supported local runner for this script
because it lacks `[System.IO.Path]::GetRelativePath`; use PowerShell 7, matching the GitHub Actions
release environment.

### Branch Protection Evidence

Before maintainer activation on 2026-04-30:

- `gh api repos/Hexalith/Hexalith.Memories/branches/main/protection` returned
  `404 Branch not protected`.
- Repository identity, default branch, authenticated account, token scope, and target branch were
  verified before treating the 404 as missing protection.

After maintainer activation on 2026-04-30:

- Repository verified as `Hexalith/Hexalith.Memories`.
- Default branch verified as `main`.
- Authenticated GitHub account verified as `QuentinDV`.
- Token scopes include `repo` and `workflow`.
- Repository permissions for the authenticated account are `pull: true`, `triage: true`,
  `push: true`, `maintain: false`, and `admin: false`.
- `gh api repos/Hexalith/Hexalith.Memories/rulesets` shows repository ruleset `main` with
  `enforcement: active`.
- `gh api repos/Hexalith/Hexalith.Memories/rules/branches/main` reports active rules for deletion
  blocking, non-fast-forward blocking, pull-request review, and required status checks.
- The active pull-request rule requires one approval.
- The active required-status-checks rule uses strict/up-to-date checks and requires `build`,
  `test-unit-contract`, and `integration-fast`.
- `gh api repos/Hexalith/Hexalith.Memories/branches/main/protection` now also reports
  `required_status_checks.strict: true`, the same three required contexts, one required approval,
  `enforce_admins.enabled: true`, `allow_force_pushes.enabled: false`, and
  `allow_deletions.enabled: false`.
- Stale approval dismissal is not enabled (`dismiss_stale_reviews: false`). This is the current
  maintainer choice because Story 12.1 required one approving review and branch-protection
  enforcement, but did not require stricter stale-review invalidation; if the team wants that
  stronger review policy, capture it as a separate governance change rather than silently changing
  the release-path contract here.

Conclusion: branch protection is **Active** for `main`.

## Inferred Workflow

For the next release:

1. Merge through a pull request targeting `main`.
2. Ensure the merge commit is a conventional commit that semantic-release can classify.
3. Ensure required checks `build`, `test-unit-contract`, and `integration-fast` are green before
   merge.
4. Ensure `tools/release-packages.json` still matches the intended package set.
5. Run `./tools/validate-release-packages.ps1` under PowerShell 7 before merging package-related
   changes.
6. Merge to `main` and watch the `Release` workflow.
7. Confirm restore, build, test, package validation, semantic-release, and package artifact upload
   succeed.
8. Confirm the new tag exists.
9. Confirm the GitHub Release contains exactly the approved package assets for the new version.
10. Confirm NuGet flat-container APIs show the new version for all approved packages.
11. Confirm the release commit uses `[skip ci]` or capture any protected-branch conflict as a
    follow-up release-strategy item.

## Unresolved Gaps

- `@semantic-release/git` still pushes a `CHANGELOG.md` release commit back to `main`. Once branch
  protection is enforced, the next release may publish packages successfully and then fail when the
  git plugin attempts that protected push.
- Story 12.5 still owns stronger partial-publish alerting. Current recovery relies on workflow logs
  and `--skip-duplicate`.
- Story 12.6 still owns the tracked `tools/test-release.ps1` baseline filter for
  `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag`.

## Recommended Next Steps

- Preserve the active `main` ruleset and re-run these checks before treating future release-path
  changes as governed:

```powershell
gh api repos/Hexalith/Hexalith.Memories/branches/main/protection
gh api repos/Hexalith/Hexalith.Memories/rules/branches/main
```

- If branch protection blocks `@semantic-release/git`, keep the protection rule intact and handle
  the release-commit strategy in a dedicated follow-up. Do not silently weaken branch protection.

## Recovery Notes

- If restore, build, or tests fail, fix through a normal pull request and rerun release by merging a
  new conventional commit to `main`.
- If package validation fails, treat `tools/release-packages.json` and
  `tools/validate-release-packages.ps1` as the source of truth. Do not manually publish around the
  validator.
- If semantic-release fails before publishing, inspect the run logs and fix repository state or
  release configuration through a pull request.
- If NuGet publishing partially succeeds, do not delete published packages from nuget.org.
  `tools/publish-nuget.ps1` uses `dotnet nuget push --skip-duplicate`, so reruns skip HTTP 409
  already-published packages and continue. Non-409 failures still require reading the Actions log to
  know which package stopped the run.
- If the semantic-release git plugin fails after package publishing, preserve the GitHub Actions
  evidence, verify the NuGet package state, and resolve the changelog/tag/release strategy in a
  follow-up story.
- If a stale tag collision occurs, do not delete tags casually. Compare the tag target, GitHub
  Release state, NuGet package state, and semantic-release logs before choosing a repair path.
