# Story 12.5: Partial-Publish Alerting

Status: ready-for-dev

Story Key: 12-5-partial-publish-alerting
Epic: 12 - First Release & Operations Foundation
Created: 2026-05-01

**Effort estimate:** ~1.5-2.5 working days.

## Story

As an operations owner,
I want the half-published-then-network-failure scenario in `tools/publish-nuget.ps1` to produce an audible signal,
so that the `--skip-duplicate` self-healing model can be retained without it becoming an undetected silent-failure path.

## Acceptance Criteria

1. Given `tools/publish-nuget.ps1` is publishing the approved NuGet package set, when one `dotnet nuget push` fails with a non-duplicate error after at least one package has succeeded, then the script captures the failed package, the push exit code, and the error output without printing `NUGET_API_KEY`.

2. Given one package push fails, when additional packages remain in the sorted package list, then the script attempts the remaining packages where safe and records each package as `pushed`, `failed`, or `not-attempted` with the reason.

3. Given publishing finishes with any failed package or an unrecoverable validation/preflight failure, then the script exits non-zero and emits a structured summary that lists the target version, package directory, pushed packages, failed packages, not-attempted packages, and manual recovery instruction.

4. Given the failure is a partial publish, then the GitHub Actions log contains a visible error annotation whose title starts with `PARTIAL PUBLISH - manual reconciliation required`.

5. Given the release workflow runs on GitHub Actions, then a partial-publish summary creates or updates a GitHub Issue in this repository using the existing `GITHUB_TOKEN`/`issues: write` path; no Slack or external webhook secret is required.

6. Given an open partial-publish issue already exists for the same version, then a rerun comments on the existing issue instead of creating a duplicate issue.

7. Given the alert fires, then the issue/comment text references `docs/dev/release-runbook.md` and tells maintainers to rerun the release workflow because `--skip-duplicate` self-heals already-published packages while retrying failed or not-attempted packages.

8. Given Story 12.5 is release-operations hardening, then it does not change package inventory, package metadata, semantic-release versioning rules, runtime source code, public API contracts, test baselines, or submodule contents.

## Tasks / Subtasks

- [ ] Task 0 - Lock the current release contract before editing (AC: 1, 8)
  - [ ] Read `.github/workflows/release.yml`, `.releaserc.json`, `tools/publish-nuget.ps1`, `tools/pack-release.ps1`, `tools/validate-release-packages.ps1`, `tools/release-packages.json`, and `docs/dev/release-runbook.md`.
  - [ ] Confirm `release.yml` already grants `issues: write` and forwards `GITHUB_TOKEN` plus `NUGET_API_KEY` only to the `Run semantic-release` step.
  - [ ] Confirm `.releaserc.json` still invokes `publish-nuget.ps1` as the `@semantic-release/exec` `publishCmd`.
  - [ ] Do not initialize or update nested submodules. Do not stage the existing `Hexalith.EventStore` submodule state unless Jerome separately asks for it.

- [ ] Task 1 - Make publish results structured and complete (AC: 1, 2, 3)
  - [ ] Update `tools/publish-nuget.ps1` so validation failure before any push still fails fast and writes a non-publish failure summary when running in CI.
  - [ ] Keep package discovery sorted by package filename and keep `--skip-duplicate`.
  - [ ] For each package push, capture stdout/stderr, `$LASTEXITCODE`, package filename, and disposition.
  - [ ] Continue to later packages after a non-zero push exit when the script can still invoke `dotnet nuget push` safely; if a failure is classified as unrecoverable for the rest of the loop, record remaining packages as `not-attempted` with a reason.
  - [ ] After the loop, throw only after the full summary is written.
  - [ ] Do not echo secret-bearing command lines. It is acceptable to print package names, version, source URL, and sanitized error text.

- [ ] Task 2 - Emit a CI-visible summary and annotation (AC: 3, 4, 7)
  - [ ] Write a machine-readable summary file, for example `artifacts/packages/release/publish-summary.json`, only when publishing fails or partially succeeds.
  - [ ] Include enough fields for the workflow issue step to render an issue body without scraping logs.
  - [ ] When `$env:GITHUB_ACTIONS` is true, emit a GitHub Actions `::error` annotation with title `PARTIAL PUBLISH - manual reconciliation required` for partial publish.
  - [ ] Append a concise Markdown section to `$env:GITHUB_STEP_SUMMARY` when available.
  - [ ] Ensure the non-GitHub local path still prints a readable summary and exits non-zero.

- [ ] Task 3 - Add GitHub Issue alerting in `release.yml` (AC: 5, 6, 7)
  - [ ] Add a post-semantic-release step that runs on failure and checks for the publish summary file.
  - [ ] Use GitHub CLI with `GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}` and `GITHUB_REPOSITORY` to create or update an issue.
  - [ ] Search for an open issue with a deterministic title such as `PARTIAL PUBLISH <version> - manual reconciliation required`.
  - [ ] If found, add a comment with the new run URL and summary. If not found, create a new issue.
  - [ ] Include the run URL, version, pushed/failed/not-attempted package lists, and runbook reference.
  - [ ] Do not add Slack or webhook integration unless the repository already has a committed non-secret webhook mechanism by implementation time.

- [ ] Task 4 - Add focused script/workflow tests (AC: 1-7)
  - [ ] Add stdlib-based tooling tests under `tests/tooling/publish_nuget/` that invoke `pwsh` with a fake `dotnet` on `PATH`.
  - [ ] Cover all-success, duplicate-compatible success, one middle package failing while later packages are attempted, pre-push validation failure, and no secret leakage in output.
  - [ ] Cover the JSON summary shape and the duplicate-issue search/title convention without calling the live GitHub API.
  - [ ] If testing the workflow step directly is too brittle, extract the issue-body rendering into a small script such as `tools/create-partial-publish-issue.ps1` and test that script with fake `gh`.
  - [ ] Keep tests independent of real nuget.org, real `NUGET_API_KEY`, and real GitHub network calls.

- [ ] Task 5 - Update operator documentation and validate the path (AC: 3, 7, 8)
  - [ ] Update `docs/dev/release-runbook.md` partial-publish recovery notes to describe the new issue/annotation behavior.
  - [ ] Keep the existing recovery model: do not delete packages from nuget.org; rerun the workflow and let 409 duplicates skip while missing packages publish.
  - [ ] Validate `tools/validate-release-packages.ps1`.
  - [ ] Run the focused tooling tests.
  - [ ] Run `npx semantic-release --dry-run --no-ci` after `npm ci` if release workflow or semantic-release-adjacent scripts changed and local Node dependencies are available.

## File Scope

Allowed files for this story:

- `tools/publish-nuget.ps1` - UPDATE. Main deliverable: structured push outcomes, sanitized summary, CI annotation, and non-zero final failure.
- `.github/workflows/release.yml` - UPDATE. Add post-failure issue create/comment step for partial-publish summaries.
- `docs/dev/release-runbook.md` - UPDATE. Document new partial-publish alert and recovery flow.
- `tests/tooling/publish_nuget/**/*.py` - NEW. Stdlib tests for the PowerShell publish and issue-alerting scripts.
- `tools/create-partial-publish-issue.ps1` - NEW optional helper only if keeping issue rendering inside workflow YAML would be brittle.
- `_bmad-output/implementation-artifacts/12-5-partial-publish-alerting.md` - UPDATE Dev Agent Record, validation evidence, and completion notes.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad workflow state transitions.

Read/verify only:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-26.md`
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md`
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-30.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/12-1-first-release-path-validation.md`
- `_bmad-output/implementation-artifacts/12-2-forbidden-default-tolerances-checklist.md`
- `_bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md`
- `_bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md`
- `.releaserc.json`
- `package.json`
- `package-lock.json`
- `tools/pack-release.ps1`
- `tools/validate-release-packages.ps1`
- `tools/release-packages.json`
- `tools/test-release.ps1`
- `CONTRIBUTING.md`

Forbidden by default:

- `src/**/*.cs`
- `tests/**/*.cs` outside `tests/tooling/publish_nuget/`
- `tools/release-packages.json`
- `tools/pack-release.ps1`
- `tools/validate-release-packages.ps1`
- `tools/test-release.ps1`
- `.releaserc.json`
- `package.json`
- `package-lock.json`
- NuGet package metadata, public API contracts, runtime behavior, CI required check names, test baseline filters, and submodule contents.

If implementation discovers that correct alerting requires changing semantic-release plugin topology, package inventory, runtime source, or secret strategy, do not absorb that silently. Record a `Scope-Override:` rationale or split the work into a follow-up story.

## Dev Notes

### Epic Context

Epic 12 turns Epic 11 retrospective findings into release-readiness guardrails. Story 12.5 closes S11-FD: partial NuGet publishing must produce an operator-visible signal rather than relying on someone noticing the failed log line and rerunning manually.

This story should preserve the valuable part of the current model: `dotnet nuget push --skip-duplicate` makes reruns safe after a partial publish because already-published packages return duplicate conflicts while missing packages can still publish. The defect is not the rerun behavior; the defect is the missing alert and structured package-by-package state.

### Current Release Surface

Current observed repo state at story creation:

- `.github/workflows/release.yml` runs on `push` to `main`, blocks `[skip ci]` and `[skip actions]`, has `permissions: contents: write`, `issues: write`, and `pull-requests: write`, and runs `npx semantic-release` with `GITHUB_TOKEN` plus `NUGET_API_KEY`.
- `.releaserc.json` uses `@semantic-release/exec`; `publishCmd` is `pwsh -NoLogo -NoProfile -File ./tools/publish-nuget.ps1 -Version ${nextRelease.version} -PackageDirectory ./artifacts/packages/release`.
- `tools/publish-nuget.ps1` currently validates packages, sorts `.nupkg` files by name, pushes each package to `https://api.nuget.org/v3/index.json` with `--skip-duplicate`, and throws on the first non-zero exit.
- Because the script throws inside the loop, alphabetically-later packages are not attempted after the first non-409 failure. The runbook currently tells maintainers to use the workflow log as the source of truth until Story 12.5 ships alerting.
- `tools/release-packages.json` currently approves seven packages: `Contracts`, `Client.Rest`, `Redis`, `Cli`, `Mcp`, `EventStore`, and `Telemetry`.

Do not replace semantic-release, do not move package publishing out of `@semantic-release/exec`, and do not publish from local machines.

### Recommended Implementation Shape

Prefer three small, testable pieces:

1. `tools/publish-nuget.ps1`
   - owns package validation, push attempts, result capture, and summary JSON
   - writes a CI annotation and step summary when running in GitHub Actions
   - exits non-zero after writing the summary if any publish failed
2. `.github/workflows/release.yml`
   - adds a failure-only step after `Run semantic-release`
   - reads the summary file and invokes GitHub CLI
3. Optional `tools/create-partial-publish-issue.ps1`
   - renders/searches/creates/comments the GitHub issue if the YAML would otherwise become hard to test

Avoid encoding the package-state parser only in YAML. YAML-only alert logic is difficult to unit-test and would recreate the "looks green but hid failure" pattern this story is trying to remove.

### Alert Channel Decision

Use GitHub Issues for this repository:

- The release workflow already has `issues: write`.
- GitHub CLI is available on GitHub-hosted runners and can use `GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}`.
- No Slack or external webhook secret is present in this repository.
- A repository issue gives maintainers a durable audit trail after the failed workflow log ages out of immediate attention.

The workflow should still fail. The issue is the alert and audit record; it is not a substitute for a red release job.

### Summary Contract

Use a stable JSON shape so tests and workflow logic do not scrape console text. Recommended fields:

```json
{
  "schemaVersion": 1,
  "status": "partial-publish",
  "version": "1.2.3",
  "packageDirectory": "artifacts/packages/release",
  "source": "https://api.nuget.org/v3/index.json",
  "startedAt": "2026-05-01T00:00:00Z",
  "completedAt": "2026-05-01T00:00:00Z",
  "pushed": ["Hexalith.Memories.Contracts.1.2.3.nupkg"],
  "failed": [
    {
      "package": "Hexalith.Memories.Cli.1.2.3.nupkg",
      "exitCode": 1,
      "error": "sanitized stderr/stdout summary"
    }
  ],
  "notAttempted": [
    {
      "package": "Hexalith.Memories.EventStore.1.2.3.nupkg",
      "reason": "unrecoverable auth failure"
    }
  ],
  "recovery": "Rerun the Release workflow; --skip-duplicate skips already-published packages and retries missing packages."
}
```

If every package fails before any successful push, classify the status as `publish-failed`, not `partial-publish`; still fail the workflow and write a summary. Only create the "PARTIAL PUBLISH" issue when at least one package was pushed and at least one package failed or was not attempted.

### Testing Strategy

Do not test against real NuGet or GitHub. Use fake command shims and temporary package files:

- Create temporary `.nupkg` placeholder files with package-like names.
- Put a fake `dotnet` executable earlier in `PATH` that records arguments and returns configured exit codes.
- Set a fake `NUGET_API_KEY` value and assert it never appears in stdout, stderr, JSON, step summary, or issue body.
- Set `GITHUB_ACTIONS=true` plus temporary `GITHUB_STEP_SUMMARY` to verify annotation/summary behavior without network calls.
- Use a fake `gh` executable for issue creation/comment tests if an issue helper script is added.

The test suite should prove the middle-package-failure case specifically: package A succeeds, package B fails, package C is still attempted, JSON lists A in `pushed`, B in `failed`, and C in either `pushed` or `failed` based on the fake exit plan.

### Previous Story Intelligence

Carry forward these Epic 12 lessons:

- Story 12.1 established the current release runbook and recorded that partial publish recovery currently depends on reading the workflow log.
- Story 12.2 made `--skip-duplicate` acceptable only with idempotency proof, recovery path, or operator-visible signal. This story supplies that signal.
- Story 12.3 scopes automation changes tightly and treats story file scope as a contract.
- Story 12.4 owns baseline failure accounting. Do not change `tools/test-release.ps1` or accepted baseline filters here.

Recent git history before story creation:

- `77d996c docs(bmad): create story 12.4 context`
- `e7fede7 docs(bmad): create story 12.3 context`
- `d97502a feat: add pre-dev hardening output files for process notes and lessons ledger`
- `018600a fix: update subproject commit reference in Hexalith.EventStore`
- `c4d5217 docs: add Code Review section with Forbidden Default Tolerances checklist to CONTRIBUTING.md`

The current pre-dev hardening run also saw a soft preflight working-tree warning for `Hexalith.EventStore`. Treat it as unrelated and do not stage or modify submodule state in this story.

### Architecture and Project Rules

- Follow the repository's existing script-first release tooling. Do not introduce a new package dependency for alerting.
- Use PowerShell because the release publish path is already PowerShell and runs under `pwsh` on Ubuntu.
- Keep shell wrappers thin and testable.
- Do not put package versions in `.csproj`; this story should not touch project files.
- Preserve GitHub Actions required check names `build`, `test-unit-contract`, and `integration-fast`.
- Keep `NUGET_API_KEY` secret handling strict: never write the value to logs, summaries, issues, JSON, or files.

### Latest Technical Information

Web verification performed on 2026-05-01 using primary sources:

- GitHub Actions workflow commands support `::error` annotations and step summaries through `GITHUB_STEP_SUMMARY`; use annotations for immediate UI visibility and summaries for readable operator detail. Source: https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions
- GitHub Actions can use GitHub CLI in workflows by setting `GH_TOKEN`; GitHub's example includes `gh issue create --title ... --body ... --repo $GITHUB_REPOSITORY`. Source: https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-github-cli
- `gh issue create` supports non-interactive title/body/repo arguments. Source: https://cli.github.com/manual/gh_issue_create
- `@semantic-release/exec` supports `prepareCmd` and `publishCmd`, matching this repository's `.releaserc.json` topology. Source: https://github.com/semantic-release/exec

### Testing Requirements

Minimum validation before review:

```powershell
./tools/validate-release-packages.ps1
python -m unittest discover -s tests/tooling/publish_nuget -p "*_test.py"
npm ci
npx semantic-release --dry-run --no-ci
```

If `npm ci` or the semantic-release dry run is blocked by local environment or network state, record the blocker in this story's Dev Agent Record and rely on focused script tests plus workflow diff review. Do not run a real `dotnet nuget push` locally.

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 12 and Story 12.5 acceptance criteria.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-26.md` - Option C and S11-FD scaffold.
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md` - S11-FD original deferred item and Pattern 3 tolerance finding.
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-30.md` - refreshed T4 partial-publish alerting action.
- `_bmad-output/implementation-artifacts/deferred-work.md` - S11-FD and 12.1 release-hardening follow-ups.
- `_bmad-output/implementation-artifacts/12-1-first-release-path-validation.md` - observed release path and current partial-publish recovery notes.
- `_bmad-output/implementation-artifacts/12-2-forbidden-default-tolerances-checklist.md` - reviewer rule for tolerant defaults.
- `_bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md` - scope discipline and `Scope-Override:` convention.
- `_bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md` - sibling release-lane governance story; keep baselines out of this scope.
- `.github/workflows/release.yml` - release job and GitHub token permissions.
- `.releaserc.json` - semantic-release `publishCmd`.
- `tools/publish-nuget.ps1` - main publish loop.
- `tools/pack-release.ps1`, `tools/validate-release-packages.ps1`, `tools/release-packages.json` - package build and inventory contracts.
- `docs/dev/release-runbook.md` - operator recovery guidance.
- GitHub Actions workflow commands docs: https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions
- GitHub Actions GitHub CLI docs: https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-github-cli
- GitHub CLI `gh issue create` manual: https://cli.github.com/manual/gh_issue_create
- semantic-release exec plugin docs: https://github.com/semantic-release/exec

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Pre-dev hardening preflight JSON `_bmad-output/process-notes/predev-preflight-latest.json` reported a soft working-tree warning only: ` M Hexalith.EventStore`.
- Story selection logic chose `12-5-partial-publish-alerting` because the ready-for-dev buffer was below target `5` and this was the first backlog story in sprint-status order.

### Completion Notes List

- Story context created on 2026-05-01.
- Discovery loaded Epic 12 Story 12.5 planning material, S11-FD deferred-work context, Epic 11 retrospectives, current release workflow, semantic-release configuration, publish/package scripts, release runbook, and prior Epic 12 story artifacts.
- The story chooses GitHub Issue alerting because `release.yml` already has `issues: write` and no repository Slack/webhook secret is present.
- The recommended implementation keeps `--skip-duplicate`, continues publish attempts where safe, writes structured package outcomes, fails the workflow, and creates/comments a durable issue for partial publish.
- No implementation tests were run during story creation; this run only created the ready-for-dev story artifact.

### File List

- `_bmad-output/implementation-artifacts/12-5-partial-publish-alerting.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-01: Created Story 12.5 and promoted it from `backlog` to `ready-for-dev`.

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created. Status set to `ready-for-dev`.
