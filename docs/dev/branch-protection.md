# Branch Protection

This repository does not currently manage GitHub branch protection as code. A maintainer must apply
the settings below after the first `CI` workflow run has published the stable status check names.

## Required Main Settings

Configure branch protection or a repository ruleset for `main`:

- Require a pull request before merging.
- Require at least one approving review.
- Require status checks to pass before merging.
- Require these exact status checks:
  - `build`
  - `test-unit-contract`
  - `integration-fast`
- Block direct pushes to `main`.
- Do not require the release workflow for PRs; package publishing is handled after merge by Story 11.2.

## Activation Sequence

1. Open the PR that adds `.github/workflows/ci.yml`.
2. Let GitHub run the workflow once so the status check names are discoverable.
3. In branch protection or repository rulesets, select `build`, `test-unit-contract`, and
   `integration-fast` as required checks.
4. Enable required pull requests, one approving review, and direct-push blocking.
5. Re-run the PR checks and confirm all required checks are enforced before merge.

If these repository settings are not yet applied, AC #6 remains pending maintainer action even though
the exact settings and activation sequence are documented here.
