# Branch Protection

This repository does not manage GitHub branch protection as code. A maintainer must apply the
settings below through the GitHub UI or `gh api`. The settings were applied for `main` on
2026-04-30; the [Applied Evidence](#applied-evidence) section records the resulting state.

## Required Main Settings

Configure classic branch protection or a repository ruleset for `main` with all of the following:

- Require a pull request before merging.
- Require at least one approving review.
- Require status checks to pass before merging.
- Require branches to be up to date before merging (`required_status_checks.strict: true`).
- Require these exact status checks:
  - `build`
  - `test-unit-contract`
  - `integration-fast`
- Include administrators in branch protection (`enforce_admins.enabled: true`) so the contract
  applies uniformly. Without this, an admin push silently bypasses the rule.
- Block force pushes (`allow_force_pushes.enabled: false`).
- Block branch deletion (`allow_deletions.enabled: false`).
- Block direct pushes to `main`. With required PR review enabled, non-admin direct pushes are
  rejected; with `enforce_admins.enabled: true`, admins are subject to the same rule. There is no
  separate `restrict_pushes` field that needs to be set on top of this combination.
- Do not require the release workflow for PRs; package publishing is handled after merge by the
  release pipeline (see [`release-runbook.md`](./release-runbook.md)).

The three required check names map to job IDs in `.github/workflows/ci.yml`:

| Required check | Source | Status |
| --- | --- | --- |
| `build` | `.github/workflows/ci.yml` job `build` (line 19) | required to pass |
| `test-unit-contract` | `.github/workflows/ci.yml` job `test-unit-contract` (line 40) | required to pass |
| `integration-fast` | `.github/workflows/ci.yml` job `integration-fast` (line 73) | required to pass |

If a job is renamed in `ci.yml`, the corresponding required check entry must be re-added through
the branch protection UI; GitHub does not auto-rename required checks.

## Activation Sequence

1. Open the PR that adds `.github/workflows/ci.yml`.
2. Let GitHub run the workflow once so the status check names are discoverable.
3. In branch protection or repository rulesets, select `build`, `test-unit-contract`, and
   `integration-fast` as required checks.
4. Enable required pull requests, one approving review, "Require branches to be up to date before
   merging", and "Include administrators".
5. Re-run the PR checks and confirm all required checks are enforced before merge.

If these repository settings are not yet applied for any reason (initial setup, migration, repo
fork), the release contract is incomplete and the release runbook's prerequisites are not
satisfied. The [Applied Evidence](#applied-evidence) section below records the state that was
applied on 2026-04-30 and that any future re-application must reproduce.

## Applied Evidence

Story 12.1 applied classic branch protection for `main` on 2026-04-30.

Before application, `gh api repos/Hexalith/Hexalith.Memories/branches/main/protection` returned
`404 Branch not protected`.

After application, the GitHub branch protection API reported:

- `required_status_checks.strict: true`
- `required_status_checks.contexts: build, test-unit-contract, integration-fast`
- `required_pull_request_reviews.required_approving_review_count: 1`
- `enforce_admins.enabled: true`
- `allow_force_pushes.enabled: false`
- `allow_deletions.enabled: false`

The combination of `required_pull_request_reviews` and `enforce_admins.enabled: true` is what
blocks direct pushes to `main`: non-admins are rejected because a PR with at least one approving
review is required, and admins are rejected because `enforce_admins` removes the admin bypass.
There is no separate API field that has to be set on top of this combination.

`gh api repos/Hexalith/Hexalith.Memories/branches/main` also reported `protected: true`.
