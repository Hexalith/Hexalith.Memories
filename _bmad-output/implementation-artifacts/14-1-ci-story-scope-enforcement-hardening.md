# Story 14.1: CI Story-Scope Enforcement Hardening

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want story-scope validation and CI diff discovery to fail loudly and parse story keys consistently,
so that future feature work cannot bypass file-scope enforcement through shallow fetches, malformed story keys, or ambiguous branch metadata.

## Acceptance Criteria

1. Given the CI story-scope job fetches the comparison base, when the fetch fails because of auth, network, repository rename, or unavailable refs, then the workflow fails loudly with a diagnostic that names the failed fetch operation and does not continue into a degraded `git diff-tree -r HEAD` fallback caused by `|| true`.

2. Given the workflow runs on a push to `main`, when the calculated diff is empty or `origin/main` resolves to the same commit as `HEAD`, then the story-scope check fails with a direct-push or empty-diff diagnostic and does not silently pass file-scope validation.

3. Given branch metadata or explicit `--story-key` input contains more than one story key, when `tools/check-story-file-scope.py` parses it, then validation rejects the input consistently with trailer multi-key rejection and reports all detected conflicting keys.

4. Given `git interpret-trailers` is unavailable, when the story-scope validator needs trailer parsing, then it raises a clean validation error with an actionable installation or `PATH` message and no raw `FileNotFoundError` stack trace is emitted.

5. Given the story-scope validator parses story files, when boundary cases are exercised for `STORY_KEY_PATTERN`, code fences, backtick paths, allow-list termination, and diagnostics, then focused tests cover those cases using the existing Python test harness and all existing story-scope tests remain green.

6. Given Story 14.1 closes deferred work, when the story is marked done, then deferred IDs `12.4-RV1` through `12.4-RV5`, `12.4-RV7` through `12.4-RV18`, and any implemented related `12.3` parser findings are removed from `_bmad-output/implementation-artifacts/deferred-work.md` or marked resolved with validation evidence.

## Tasks / Subtasks

- [ ] Task 1 - Harden CI changed-file discovery and fetch failure behavior (AC: 1, 2)
  - [ ] Remove any `git fetch ... || true` pattern from the `story-file-scope` job. Fetch/auth/ref failures must stop the job with the failed operation named in output.
  - [ ] Replace the shallow `origin/main..."$head_sha"` fallback with deterministic comparison logic that either fetches the needed base history or fails loudly when the base cannot be resolved.
  - [ ] Detect push-to-main or same-commit `origin/main == HEAD` cases before invoking the validator and emit a direct-push/empty-diff diagnostic.
  - [ ] Treat an empty changed-file list from CI as an error unless the job can prove it is an intentional no-op path. The existing validator's local no-op success must remain valid for hooks and direct CLI use.
  - [ ] Keep PR validation on the real PR head commit and source branch; do not validate the synthetic merge commit message.

- [ ] Task 2 - Make story-key extraction symmetric across CLI, trailers, and branch metadata (AC: 3)
  - [ ] Update explicit `--story-key` parsing to use the same "all detected keys" behavior as trailer parsing, rejecting values with zero keys or more than one key.
  - [ ] Update branch-name parsing to reject branch names containing more than one distinct story key instead of silently choosing the first match.
  - [ ] Preserve existing precedence when all sources agree: CLI first, trailer second, branch third.
  - [ ] Report every conflicting key and source in the diagnostic so contributors can fix the branch name, trailer, or CI input without guessing.

- [ ] Task 3 - Wrap `git interpret-trailers` absence and trailer-parser failures cleanly (AC: 4)
  - [ ] Catch `FileNotFoundError` around `git interpret-trailers --parse` and convert it to `ValidationError`.
  - [ ] The message must say that Git with `interpret-trailers` is required and advise checking the Git installation or `PATH`.
  - [ ] Keep non-zero `git interpret-trailers --parse` results as validation failures with stderr preserved, but without Python stack traces.

- [ ] Task 4 - Strengthen parser and diagnostic boundary coverage (AC: 3, 4, 5)
  - [ ] Add tests for multi-key `--story-key` values and multi-key branch names.
  - [ ] Add `STORY_KEY_PATTERN` edge tests for a single-letter title segment, trailing hyphen rejection, uppercase input normalization, adjacent word/hyphen boundaries, and multiple-key reporting.
  - [ ] Add parser tests for code fences longer than three backticks, nested fence-like content, bare-token bullet diagnostics, multiple `Allowed files for this story:` blocks, and termination only on known labels or headings.
  - [ ] Add tests that assert fixture-based scope parsing reports the loaded story artifact path so loader-precedence regressions are visible.
  - [ ] Harden test assertions so validation errors can move between stdout and stderr without creating brittle false failures.

- [ ] Task 5 - Update contributor guidance and deferred-work bookkeeping (AC: 1, 2, 5, 6)
  - [ ] Update `CONTRIBUTING.md` only where behavior changes: CI empty-diff/direct-push handling, multi-key branch/CLI rejection, and clean trailer-parser failure requirements.
  - [ ] Mark resolved or remove target deferred entries in `_bmad-output/implementation-artifacts/deferred-work.md` with concise evidence after validation passes.
  - [ ] Do not close target deferred entries that remain intentionally unimplemented; carry them forward with a fresh rationale and re-open trigger instead.

- [ ] Task 6 - Validate the story-scope lane (AC: 1-6)
  - [ ] Run `python tools/check-story-file-scope.py --help`.
  - [ ] Run `python -m unittest discover -s tests/tooling/story_scope -p "*_test.py"`.
  - [ ] Run focused manual validator probes for in-scope, out-of-scope, multi-key CLI, multi-key branch, empty CI changed-file behavior, and missing `git interpret-trailers` where feasible.
  - [ ] If `.github/workflows/ci.yml` changes are non-trivial, use `git diff --check -- .github/workflows/ci.yml tools/check-story-file-scope.py tests/tooling/story_scope/story_scope_validator_test.py CONTRIBUTING.md _bmad-output/implementation-artifacts/deferred-work.md`.

## File Scope

Allowed files for this story:

- `.github/workflows/ci.yml` - UPDATE. Harden `story-file-scope` changed-file discovery, fetch failure handling, and direct-push/empty-diff diagnostics.
- `tools/check-story-file-scope.py` - UPDATE. Make story-key parsing symmetric and wrap `git interpret-trailers` failures cleanly.
- `tests/tooling/story_scope/story_scope_validator_test.py` - UPDATE. Add focused regressions for CI input behavior, story-key parsing, parser boundaries, and diagnostics.
- `CONTRIBUTING.md` - UPDATE. Align story-scope guidance with the hardened behavior.
- `_bmad-output/implementation-artifacts/deferred-work.md` - UPDATE. Resolve, remove, or explicitly carry forward Story 14.1 target deferred IDs with evidence.
- `_bmad-output/implementation-artifacts/14-1-ci-story-scope-enforcement-hardening.md` - UPDATE. Record implementation notes, validation, review findings, and file list.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad workflow/status transitions.

Read/verify only:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-03.md`
- `_bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md`
- `_bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md`
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-30.md`
- `_bmad-output/implementation-artifacts/epic-12-retro-2026-05-02.md`
- `.githooks/pre-commit`
- `.githooks/commit-msg`

Forbidden by default:

- `src/**/*.cs`
- `tests/**/*.cs`
- `tools/publish-nuget.ps1`
- `tools/pack-release.ps1`
- `tools/test-release.ps1`
- `package-lock.json`
- `Hexalith.AI.Tools/**`
- `Hexalith.Commons/**`
- `Hexalith.EventStore/**`

## Dev Notes

### Current Implementation State

Story 12.3 created one shared Python validator at `tools/check-story-file-scope.py`, thin local hooks under `.githooks/`, a `story-file-scope` job in `.github/workflows/ci.yml`, and focused stdlib `unittest` coverage under `tests/tooling/story_scope/`. Keep that architecture: shell/YAML may gather inputs, but story-key precedence, trailer parsing, file-scope parsing, forbidden-default handling, overrides, and diagnostics belong in the Python validator.

The current CI job checks out the PR or push head with `fetch-depth: 0`, derives PR branch metadata from `github.head_ref`, reads the real PR head commit message from `github.event.pull_request.head.sha`, and delegates validation to the Python tool. That PR-head behavior must be preserved because GitHub documents that `pull_request` workflows otherwise use the merge branch and merge SHA by default.

The known weak spot is the push fallback path in `.github/workflows/ci.yml`. Deferred findings record that the fallback can swallow fetch failure, rely on shallow merge-base semantics, or silently produce an empty diff when validating direct pushes to `main`. This story should turn those cases into loud, actionable failures rather than broader release or runtime changes.

### Deferred IDs Targeted

This story is the normal lifecycle home for:

- `12.4-RV1` through `12.4-RV5`: CI fetch/fallback/direct-push/branch-env hardening in the `story-file-scope` job.
- `12.4-RV7` through `12.4-RV18`: story-key parser symmetry, regex boundary coverage, parser diagnostics, story path diagnostics, Markdown fence/termination behavior, `git interpret-trailers` absence, and test hardening.
- Related `12.3` parser findings when they overlap the ACs: especially clean message decoding/changed-file BOM handling, allowed-label/documentation alignment, multiple allow-list block detection, and parser termination behavior.

Do not treat every duplicate or repeated deferred-work line as a separate product requirement. Normalize or resolve duplicates consistently when editing `deferred-work.md`.

### Implementation Guardrails

- Do not broaden this story into release publishing, package inventory, OIDC, embedding, migration, integration-test, runtime source, or submodule changes. Those belong to Stories 14.2-14.4 or already completed epics.
- Do not initialize or update nested submodules. Root-level submodule pointers are forbidden by default for this story unless a human explicitly scopes the change.
- Do not add a new hook framework, Python package dependency, or GitHub Action dependency unless there is no viable standard-library/Git/YAML option.
- Keep diagnostics plain text and contributor-facing. The validator is used locally and in CI, so messages should name the failed input source, selected story source, changed-file source, or missing tool directly.
- Preserve existing local-hook no-op behavior: `python tools/check-story-file-scope.py` with no changed files may remain a successful explicit no-op. The CI job should fail on empty diff because CI empty-diff is the bypass under review.

### Technical Constraints and References

- Git `interpret-trailers --parse` is still the right trailer parser; it avoids hand-rolled footer parsing. Wrap absence or failure rather than replacing it with ad hoc parsing. Source: https://git-scm.com/docs/git-interpret-trailers
- GitHub Actions contexts document `github.head_ref` as the PR source branch and `github.ref` as the pull request merge branch for non-merged PRs. Source: https://docs.github.com/en/actions/reference/workflows-and-actions/contexts
- GitHub Actions events documentation says `GITHUB_SHA` for `pull_request` is the merge-branch SHA and recommends `github.event.pull_request.head.sha` to test the head branch only. Source: https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows
- GitHub Actions environment-file docs allow multiline values with a delimiter, but warn that the delimiter must not occur alone in the value. If branch names remain written through `$GITHUB_ENV`, use a delimiter that cannot collide or write arbitrary values to files instead. Source: https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-commands

### Testing Requirements

Minimum validation before review:

```powershell
python tools/check-story-file-scope.py --help
python -m unittest discover -s tests/tooling/story_scope -p "*_test.py"
git diff --check -- .github/workflows/ci.yml tools/check-story-file-scope.py tests/tooling/story_scope/story_scope_validator_test.py CONTRIBUTING.md _bmad-output/implementation-artifacts/deferred-work.md
```

Manual probes to record in completion notes:

- CI fetch failure path fails loudly without `|| true` fallback.
- Push-to-main or empty CI diff fails loudly.
- `--story-key "14-1-ci-story-scope-enforcement-hardening and 12-3-story-file-scope-enforcement"` fails and reports both keys.
- Branch names containing multiple story keys fail and report all detected keys.
- Missing `git interpret-trailers` produces a clean `ValidationError` message rather than a Python stack trace.
- Existing happy paths remain green: in-scope files pass, out-of-scope files fail, matching non-forbidden `Scope-Override:` passes, forbidden-default files fail even with override.

## Project Structure Notes

- This is a tooling and governance story. Expected implementation is limited to `.github/workflows`, `tools`, `tests/tooling/story_scope`, `CONTRIBUTING.md`, and BMAD artifacts.
- The repository already uses lightweight script-based tooling. Mirror the existing Python stdlib test harness instead of adding new dependencies.
- The `Hexalith.Commons` `project-context.md` discovered by the persistent-facts glob is background convention only because it belongs to a submodule/sibling repository. Story-local file scope and repository-specific artifacts take precedence.

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 14 and Story 14.1 acceptance criteria.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-03.md` - approved Epic 14 scope and story ordering.
- `_bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md` - original validator, hook, CI, File Scope, and review decisions.
- `_bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md` - source of target deferred findings from the review close-out.
- `_bmad-output/implementation-artifacts/deferred-work.md` - target deferred IDs and closure bookkeeping.
- `.github/workflows/ci.yml` - current `story-file-scope` job.
- `tools/check-story-file-scope.py` - shared validator entrypoint.
- `tests/tooling/story_scope/story_scope_validator_test.py` - current stdlib regression coverage.
- `CONTRIBUTING.md` - contributor-facing story-scope contract.
- Git interpret-trailers docs: https://git-scm.com/docs/git-interpret-trailers
- GitHub Actions contexts docs: https://docs.github.com/en/actions/reference/workflows-and-actions/contexts
- GitHub Actions pull_request event docs: https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows
- GitHub Actions workflow command docs: https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-commands

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Pre-dev hardening preflight passed on 2026-05-03T10:02:28Z with all checks green and `0 dirty paths`.
- Story selection chose `14-1-ci-story-scope-enforcement-hardening` because `ready_count` was below the target of `5` and this was the first backlog story in sprint-status order.
- `/bmad-create-story 14-1-ci-story-scope-enforcement-hardening` context gathering loaded Epic 14 planning, the approved 2026-05-03 sprint-change proposal, Story 12.3, current CI workflow, validator, tests, contributor guidance, deferred-work entries, recent git history, and official Git/GitHub documentation.

### Completion Notes List

- Story context created on 2026-05-03.
- The story deliberately keeps scope on the existing story-scope guardrail lane and does not reopen runtime, release-publishing, provider, migration, integration, package-lock, or submodule work.
- The implementation guidance distinguishes local no-op validation from CI empty-diff bypass detection.
- Target deferred IDs and likely duplicate deferred-work entries are called out so implementation can resolve bookkeeping without losing evidence.
- Party-mode review on 2026-05-03 kept the story `ready-for-dev` and clarified implementation constraints: preserve PR-head SHA validation, model CI/local empty-diff behavior explicitly, centralize story-key extraction semantics, surface `git interpret-trailers` absence as a clean validation error, and close or carry forward every targeted deferred ID with evidence.

### File List

- `_bmad-output/implementation-artifacts/14-1-ci-story-scope-enforcement-hardening.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-03: Created Story 14.1 and promoted it from `backlog` to `ready-for-dev`.
- 2026-05-03: Party-mode review completed; constraints triaged and recorded for development.

## Party-Mode Review

- ISO date and time: 2026-05-03T15:31:15+02:00
- Selected story key: 14-1-ci-story-scope-enforcement-hardening
- Command/skill invocation used: `/bmad-party-mode 14-1-ci-story-scope-enforcement-hardening; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - Keep PR validation anchored to `github.event.pull_request.head.sha` and avoid regressions to synthetic merge-commit validation.
  - Make the CI/local empty-diff split explicit in implementation and tests: CI empty diff fails, local no-op validation remains successful.
  - Centralize story-key extraction/rejection semantics so CLI, branch metadata, and trailers do not drift on zero-key, malformed, repeated, or multi-key inputs.
  - Preserve original Git command/ref/exit/stderr context in fetch-failure diagnostics, and expose missing `git interpret-trailers` as a clean `ValidationError` with remediation guidance.
  - Treat deferred-work closure as evidence-bearing bookkeeping: each targeted ID must be resolved or carried forward with rationale and successor reference.
- Changes applied:
  - Added this review trace and completion-note clarification to the story artifact.
  - Left acceptance criteria, file scope, and sprint status unchanged.
- Findings deferred:
  - Full GitHub Actions workflow simulation may remain manual unless existing infrastructure makes it cheap; validator-level CI-mode behavior should still be automated.
  - Broader validator refactors, new fixture files, contributor examples, and unrelated workflow modernization remain out of scope unless required by the listed ACs.
- Final recommendation: ready-for-dev

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created. Status set to `ready-for-dev`.
