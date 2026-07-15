# Release Runbook

This runbook records the observed first release path (`v1.2.0`, 2026-04-30) and the repeatable
path for the next release. Package and container publishing remains CI-only. Do not publish release
artifacts from a workstation.

## Current Release Contract

- Releases are produced by `.github/workflows/release.yml` on `push` events targeting `main`. With
  branch protection now enabled, those `push` events come from PR-merge commits, not direct pushes
  from a workstation.
- The release job restores, builds, runs `tools/test-release.ps1`, installs npm tooling, validates
  package inventory, runs the release preflight, and runs `npx semantic-release`. Semantic-release
  then drives the prepare, publish, and post-publish phases through its plugin chain —
  `pack-release.ps1` and
  `publish-release.ps1` are not invoked as separate workflow steps; they are invoked by
  `@semantic-release/exec` during the corresponding semantic-release lifecycle phase.
- Semantic-release uses `.releaserc.json`.
- Release tooling restore uses `npm ci` from the tracked root `package-lock.json`. `npm ci` is the
  release contract because it fails when `package.json` and the lockfile disagree and removes any
  existing `node_modules` tree instead of reusing workstation state.
- `tools/release-preflight.ps1` runs after package inventory validation and before `npx
  semantic-release`. It executes the existing `release:dry-run` script to read the next version
  from semantic-release without running prepare or publish hooks, converts that version through
  `.releaserc.json` `tagFormat: "v${version}"`, and checks exact local and remote tag refs such as
  `refs/tags/v1.2.3`. It intentionally does not treat similarly prefixed tags such as
  `v1.2.30` as collisions.
- Semantic-release runs `tools/verify-container-registry.ps1` as its `verifyReleaseCmd`, before
  prepare creates release artifacts or a tag can be published. The verifier first requires the
  unauthenticated `/v2/` response to advertise a Basic `WWW-Authenticate` realm, then opens and
  immediately cancels an authenticated OCI upload session in both `memories` and `memories-mcp`.
  The challenge check proves challenge-driven clients can select the authfile credential; the
  upload probes separately prove write authorization. Every opened probe session must return HTTP
  204 when cancelled or the release fails closed.
- `tools/pack-release.ps1` is the `prepareCmd` for `@semantic-release/exec`.
- `tools/publish-release.ps1` is the `publishCmd` for `@semantic-release/exec`. It always attempts
  both NuGet and container publication and writes one aggregate release summary.
- `tools/pack-release.ps1` prebuilds the Server and MCP OCI archives and the versioned production
  deployment before any publish-side effects occur.
- `tools/publish-containers.ps1 -Push` publishes the prebuilt archives with `skopeo copy`, passing
  credentials through a scoped temporary authfile built from `HEXALITH_ZOT_USERNAME` and
  `HEXALITH_ZOT_API_KEY` and deleted after publication. The docker daemon must not be used to push
  to this registry: the registry allows anonymous read, so zot answers the daemon's
  unauthenticated `GET /v2/` ping with HTTP 200, the daemon caches "no auth required", and it then
  never sends credentials on push, failing with `unauthorized: authentication required`
  (project-zot/zot#2928). skopeo uses the Basic challenge preserved by Zot's `/v2/` response and
  the scoped authfile. It also fails if an ingress replaces that response with a synthetic `200`
  that omits `WWW-Authenticate`, so the release verifier checks this server-side precondition.
  Remote tag reconciliation compares the archive and remote manifest config digests through the
  same skopeo path. The local side of that comparison is the prebuilt archive manifest's config
  digest (the archives are single-platform), no longer a daemon-loaded image ID.
- Semantic-release does not commit generated files back to `main`. It creates the release tag,
  GitHub Release, and package assets without using `@semantic-release/git`, so the release path is
  compatible with the protected-branch rule that requires pull requests for repository changes.
- `tools/validate-release-packages.ps1` is the canonical package verifier (used both before
  semantic-release runs and inside `pack-release.ps1` after pack).
- `tools/release-packages.json` is the approved package inventory.
- The current inventory contains nine approved packages: Aspire, Contracts, Client.Rest, Redis,
  Cli, Mcp, ServiceDefaults, EventStore, and Telemetry. The historical `v1.2.0` evidence below
  remains a record of the seven packages published by that release.
- `NUGET_API_KEY` is provided only through `secrets.NUGET_API_KEY` in GitHub Actions and is forwarded
  to the release job as an environment variable; it is never written to a file in the repo.

The required-check names mapping to `.github/workflows/ci.yml` job IDs is documented in
[`branch-protection.md`](./branch-protection.md).

### Release Skip Instructions

`.github/workflows/release.yml` does not carry its own job-level parser over
`github.event.head_commit.message`. GitHub's native push skip handling is the release contract:
if the merged commit message contains a bracketed skip instruction such as `[skip ci]`,
`[ci skip]`, `[no ci]`, `[skip actions]`, or `[actions skip]`, GitHub may skip the workflow before
the release job can run.

Treat the final merged commit message as a full-message input, including the subject, body,
quoted examples, copied changelog text, squash bodies, and revert text. Do not place a bracketed
skip instruction anywhere in a release-eligible merge or squash commit message unless the intended
outcome is to suppress the release workflow. Unbracketed prose such as `skip ci` is not the
repository-owned skip contract, but maintainers should still avoid ambiguous wording in release
PR titles and squash messages.

This is an accepted GitHub Actions platform risk rather than an in-workflow guardrail: a workflow
that GitHub skips natively cannot run a repository-owned validator. If a release is silently
skipped because quoted text contained a bracketed skip instruction, open a normal PR that removes
or rewrites the token from the final merge message and merge a new conventional commit to trigger
release again.

## Prerequisites

Before relying on the release path:

1. `main` branch protection is enabled (see [`branch-protection.md`](./branch-protection.md)).
2. Pull requests are required before merge.
3. At least one approving review is required.
4. Required checks are exactly `build`, `test-unit-contract`, and `integration-fast`.
5. Direct pushes are blocked by the protected-branch policy; the only path to producing a `push`
   event on `main` is merging a PR.
6. A scoped `NUGET_API_KEY` repository secret and the standard `HEXALITH_ZOT_USERNAME` and
   `HEXALITH_ZOT_API_KEY` secrets exist. The Zot principal has repository write authorization for
   both flat repositories, `memories` and `memories-mcp`. `HEXALITH_ZOT_REGISTRY` may override the
   default `registry.hexalith.com` host.
7. The `skopeo` CLI is on the release runner's PATH (preinstalled on the GitHub-hosted
   `ubuntu-24.04` image). Container publication fails closed with disposition `tooling-missing`
   when it is absent.
8. The merge to `main` uses a conventional commit that semantic-release can classify.
9. The local working tree is clean before opening the release PR — no uncommitted edits, no
   in-flight tag drift, no orphan packages in `artifacts/`. The release job builds from the merged
   commit; clean local state is what makes the merged commit predictable.
10. `./tools/validate-release-packages.ps1` passes before release.
11. `tools/release-packages.json` still lists the intended package set.

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
2. The release job checked out the repository (root-declared `references/` submodules enabled;
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

Story 12.1 initially identified a protected-branch compatibility risk because the original
`.releaserc.json` used `@semantic-release/git` to commit `CHANGELOG.md` back to `main`. The
post-merge release run for PR #12 confirmed that failure mode: GitHub rejected the direct push to
`main` with rule violation `GH013`. The current release configuration removes the changelog/git
commit plugins, so future releases should not attempt `HEAD:main` pushes from the release job.

## Release Identity And Forensic Anchors

Release-time writes (tag, GitHub Release, package upload) are produced under
`secrets.GITHUB_TOKEN`. In a GitHub Actions run, `GITHUB_TOKEN` is minted by the GitHub Actions
GitHub App (`github-actions`, `https://github.com/apps/github-actions`) and posts as the bot user
`github-actions[bot]` (GitHub user id `41898282`). This is the identity reviewers should pin in any
forensic comparison; the historical "semantic-release-bot" display recorded for `v1.2.0` was the
author name configured by `@semantic-release/git` for the legacy release commit and is not the
underlying token identity.

For each release, capture the following forensic anchors so a future review can confirm the
release was driven by Actions and not by a workstation:

- The Actions run URL of the `Release` workflow run that produced the tag
  (`github.com/Hexalith/Hexalith.Memories/actions/runs/<run-id>`).
- The tag commit SHA (`git rev-parse v<version>` once the tag exists locally) and its tagger
  identity. With the post-12.1 release config that no longer uses `@semantic-release/git`, the
  tag is created via the GitHub API and is attributed to `github-actions[bot]`.
- The GitHub Release "Created by" user — must be `github-actions[bot]`. Anything else (a personal
  user, a non-github-actions GitHub App) is a forensic red flag and must be investigated before
  any further publish action.
- Whether the workflow run was triggered by a `push` event on `main` (the only allowed release
  trigger). `workflow_dispatch` or other event names are also red flags.

If any of those checks fail, treat the release as untrusted, do not delete published packages
from nuget.org, and escalate before merging another release-eligible commit to `main`.

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

### Per-Release Package Audit Evidence

`tools/validate-release-packages.ps1` proves the inventory and metadata shape of the generated
packages, but it does not by itself prove byte-identity or signature provenance. For every
release starting after Story 14.2 closes, capture an explicit per-package audit record alongside
the `Package Evidence` table. Choose at least one of the following options and record the exact
commands that produced the evidence in the release Pull Request body or the GitHub Release notes:

1. **SHA-256 checksums (preferred default).** Hash every `.nupkg` produced under
   `artifacts/packages/release/` and pair the hash with the package id and version. Both
   commands below return the same SHA-256 value for the same input file, so Windows and Linux
   reviewers can compare results directly:

   ```powershell
   # Windows runners (pwsh 7+, Windows PowerShell 5.1)
   Get-ChildItem artifacts/packages/release -Filter *.nupkg |
     ForEach-Object { Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName } |
     Select-Object Hash, Path
   ```

   ```bash
   # Linux/macOS runners
   ( cd artifacts/packages/release && sha256sum *.nupkg )
   ```

   Both forms emit a deterministic `Hash  Path` (PowerShell) or `<hash>  <name>` (sha256sum)
   record. Paste the resulting block into the release evidence under the package table.

2. **`dotnet nuget verify --all` evidence.** When the release introduces or rotates package
   signing, the equivalent provenance check is signature verification. Run the command per
   `.nupkg` and record the `Successfully verified` / `Signature type` lines as audit output.
   Signature verification proves origin; it does not replace SHA-256 for byte-level identity.

   ```powershell
   Get-ChildItem artifacts/packages/release -Filter *.nupkg |
     ForEach-Object { dotnet nuget verify --all $_.FullName }
   ```

3. **`nuget verify -Signatures`.** Functional equivalent to option 2 when only the standalone
   `nuget` CLI is available; record the same successful-verification output.

The historical `v1.2.0` package set is not retroactively backfilled with SHA-256 evidence
because the CI build that produced those `.nupkg` files is no longer available locally; use the
NuGet flat-container API entries plus the existing `Package Evidence` table as the audit record
for that specific version. Future releases must produce per-release evidence before the GitHub
Release is treated as final.

Package publishing remains CI-only. Operators must not run `dotnet nuget push`, manual checksum
spot-checks, or signature spoof checks from a workstation against the canonical NuGet source —
those actions can move the published state away from the CI-recorded audit anchors above.

## Second Release Checklist

1. Merge only through a pull request targeting `main`.
2. Confirm the PR has at least one approval.
3. Confirm required checks `build`, `test-unit-contract`, and `integration-fast` passed.
4. Confirm the merge commit follows conventional-commit semantics.
5. Confirm `tools/release-packages.json` has not drifted.
6. Run `./tools/validate-release-packages.ps1` locally before merging if package files changed.
7. Confirm the final merge or squash commit message does not contain bracketed skip instructions.
8. Merge to `main`.
9. Watch the `Release` workflow run.
10. Confirm `Install release tooling` used `npm ci` from the tracked lockfile.
11. Confirm `Run release preflight` succeeded before `Run semantic-release`.
12. Confirm semantic-release's registry write-scope verification opened and cancelled upload
    sessions for both repositories before prepare/publish work began.
13. If the release preflight fails on a stale tag, confirm the exact blocked ref and resolve the
    tag only after release-owner review.
14. Confirm `Build`, `Test unit and non-Docker suite`, `Validate package inventory before release`,
   `Run release preflight`, `Run semantic-release`, and `Upload package artifacts` all succeed.
15. Confirm the new tag exists.
16. Confirm the GitHub Release contains the nine approved package assets and the versioned
    `hexalith-memories-production.yaml` deployment asset.
17. Confirm NuGet flat-container APIs show the new version for all approved packages.
18. Confirm semantic-release created the new tag and GitHub Release without attempting to push a
    release commit back to `main`.

## Failure And Recovery Notes

- If restore, build, or test fails, fix through a normal pull request and rerun release by merging
  a new conventional commit to `main`.
- If package validation fails, treat `tools/release-packages.json` and
  `tools/validate-release-packages.ps1` as the source of truth. Do not manually publish around the
  validator.
- If semantic-release fails before publishing, inspect the run logs and fix the repository state or
  release configuration through a pull request.
- If registry verification reports a missing Basic `WWW-Authenticate` realm, stop before creating
  a release. Confirm `/v2/` is routed to Zot and that no ingress or health-check sidecar replaces
  its response. Do not work around the failure by making clients send Basic credentials
  preemptively.
- If registry write-scope verification returns HTTP 401 or 403, stop before creating a release.
  An organization or Zot administrator must grant the `HEXALITH_ZOT_USERNAME` principal push
  authorization to both `memories` and `memories-mcp`; rotating a valid credential or proving
  registry login/read access does not repair a repository ACL. Never paste the username, API key,
  or authorization header into an issue or workflow log.
- If container pushes fail with `unauthorized: authentication required` after verification passes,
  inspect Zot logs for the failed request before changing credentials. No `Authorization` header
  means either `/v2/` challenge headers were changed after preflight, the skopeo authfile path was
  bypassed, or a docker-daemon push path was reintroduced. An `Authorization` header with HTTP 401
  or 403 instead points to credential or repository-policy rejection. Preserve the skopeo path and
  compare the live `/v2/` response with the verifier evidence before choosing a repair.
- If `tools/release-preflight.ps1` reports that `refs/tags/v<version>` already exists locally or on
  `origin`, stop before publish work starts. The message names the exact conflicting ref. Do not
  delete the remote tag casually; confirm whether it came from an aborted/manual release, record the
  release-owner decision, then either remove the stale tag through the approved repository process
  or merge a new conventional commit that produces a different semantic-release version.
- If semantic-release reports `GH013` or another protected-branch rejection for `HEAD:main`, keep
  branch protection intact. The release configuration should not use plugins that commit back to
  `main`; fix the release configuration through a pull request and rerun by merging a conventional
  commit.
- If NuGet publishing partially succeeds, do not delete published packages from nuget.org. There
  are two failure shapes to keep separate:
  - **HTTP 409 (already published).** `tools/publish-nuget.ps1` calls `dotnet nuget push
    --skip-duplicate`, which downgrades 409 to a warning and continues with the next package. A
    rerun therefore skips already-published packages without errors.
  - **Non-409 failures (auth, network, server error).** `dotnet nuget push` exits non-zero.
    `publish-nuget.ps1` records the failed package, exit code, and sanitized output, then continues
    to later packages where it can still safely invoke `dotnet nuget push`. If a preflight or
    unrecoverable condition prevents more attempts, the remaining packages are listed as
    not-attempted with a reason.
- `tools/publish-nuget.ps1` records a structured package-by-package summary at
  `artifacts/packages/release/publish-summary.json` before it exits non-zero. The summary lists the
  target version, package directory, source, pushed packages, failed packages with exit codes and
  sanitized output, and not-attempted packages.
- When at least one package pushed and at least one package failed or was not attempted, the
  release log includes a GitHub Actions error annotation titled
  `PARTIAL PUBLISH - manual reconciliation required`. The failed job also appends a concise
  Markdown summary to the GitHub Actions step summary when that output is available.
- The release workflow creates a GitHub Issue titled
  `PARTIAL PUBLISH <version> - manual reconciliation required`. If an open issue already exists
  for the same version, reruns add a comment to the existing issue instead of creating a duplicate.
  The issue/comment includes the run URL, version, pushed/failed/not-attempted package lists, and
  this runbook reference.
- Once a failed publish has created the exact version tag or published any NuGet package, do not
  rerun normal semantic-release for that version and do not delete or republish packages. Dispatch
  **Recover Partial Release** from `main` with the existing bare version (for example, `2.6.6`).
  The workflow accepts only a semantic-version tag reachable from `origin/main` and never creates
  or moves a tag. It stages the recovery scripts and their passing fixtures from current trusted
  `main`, then checks out and builds the exact tagged source.
- Recovery first proves Zot write scope for both repositories. It then rebuilds and reconciles the
  Server and MCP immutable tags using the remote image **config digest**. A matching tag is recorded
  as already present, a missing tag is pushed, and any conflicting digest fails closed.
- Recovery never invokes a NuGet push. It downloads the nine expected packages from the NuGet flat
  container, validates their nuspec id/version against the tagged inventory, hashes them, and uses
  those verified bytes as GitHub Release assets. It creates or completes the stable GitHub Release
  with exactly those nine `.nupkg` files plus `hexalith-memories-production.yaml`. Existing matching
  assets are retained; unexpected names or mismatched bytes fail closed.
- Only after the two images, nine packages, versioned deployment, and all ten GitHub Release assets
  verify does recovery attach the evidence artifact to the matching partial-publish incident and
  close it. A failed or interrupted run remains safely rerunnable and leaves the incident open.
- Container release members are prebuilt during prepare. On a rerun, an existing immutable image
  tag is accepted only when its remote config digest matches the prebuilt archive; a conflicting
  digest fails closed and requires release-owner reconciliation.
- `tools/publish-release.ps1` writes the cross-family audit record to
  `artifacts/release/publish-summary.json`. The release workflow uploads that aggregate summary and
  its child summaries even on failure, and creates one reconciliation issue for an aggregate
  `partial-publish` state.
- If `tools/test-release.ps1` reports an unexpected test failure, check whether the failure name
  is on the project's tracked-baseline list before treating it as a release blocker. The script
  filters `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag`
  (in `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/`) per S11-FA; the resolution of S11-FA
  is owned by Story 12.6, not by this runbook.
- Never print, copy, or store the `NUGET_API_KEY` value in logs, docs, screenshots, or repo files.
