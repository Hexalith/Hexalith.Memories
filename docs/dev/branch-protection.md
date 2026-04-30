# Branch Protection

This repository does not currently manage GitHub branch protection as code. A maintainer with admin
rights must preserve the settings below after the first `CI` workflow run has published the stable
status check names.

## Required Main Settings

Configure classic branch protection or an active repository ruleset for `main`:

- Require a pull request before merging.
- Require at least one approving review.
- Require status checks to pass before merging.
- Require branches to be up to date before merging (`required_status_checks.strict: true`).
- Require these exact status checks:
  - `build`
  - `test-unit-contract`
  - `integration-fast`
- Include administrators in enforcement where repository settings allow it.
- Block direct pushes to `main`.
- Block force pushes.
- Block branch deletion.
- Do not require the release workflow for PRs; package publishing is handled after merge by Story 11.2.

## Activation Sequence

1. Open the PR that adds `.github/workflows/ci.yml`.
2. Let GitHub run the workflow once so the status check names are discoverable.
3. In branch protection or repository rulesets, select `build`, `test-unit-contract`, and
   `integration-fast` as required checks.
4. Enable required pull requests, one approving review, "Require branches to be up to date before
   merging", administrator enforcement, direct-push blocking, force-push blocking, and branch
   deletion blocking.
5. Re-run the PR checks and confirm all required checks are enforced before merge.

If these repository settings are removed or disabled, AC #6 becomes pending maintainer action even
though the exact settings and activation sequence are documented here.

## Current Evidence

Story 12.1 rechecked the repository settings on 2026-04-30 after maintainer activation:

- Repository identity: `Hexalith/Hexalith.Memories`.
- Default branch: `main`.
- Authenticated account: `QuentinDV`.
- Authenticated repository permissions: push/write but not maintain/admin.
- Repository rulesets endpoint showed ruleset `main` (`id: 15760772`) with `enforcement: active`.
- Active branch rules for `main` reported:
  - `deletion`
  - `non_fast_forward`
  - `pull_request` with `required_approving_review_count: 1`
  - `required_status_checks` with `strict_required_status_checks_policy: true`
  - required checks `build`, `test-unit-contract`, and `integration-fast`
- Classic branch-protection endpoint also reported:
  - `required_status_checks.strict: true`
  - `required_status_checks.contexts: build, test-unit-contract, integration-fast`
  - `required_pull_request_reviews.required_approving_review_count: 1`
  - `enforce_admins.enabled: true`
  - `allow_force_pushes.enabled: false`
  - `allow_deletions.enabled: false`

Status: **Active**. The branch-protection contract is enforced for `main`.
