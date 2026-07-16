# Story 14.1: CI Story-Scope Enforcement Hardening

Status: done

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

- [x] Task 1 - Harden CI changed-file discovery and fetch failure behavior (AC: 1, 2)
  - [x] Remove any `git fetch ... || true` pattern from the `story-file-scope` job. Fetch/auth/ref failures must stop the job with the failed operation named in output.
  - [x] Replace the shallow `origin/main..."$head_sha"` fallback with deterministic comparison logic that either fetches the needed base history or fails loudly when the base cannot be resolved.
  - [x] Detect push-to-main or same-commit `origin/main == HEAD` cases before invoking the validator and emit a direct-push/empty-diff diagnostic.
  - [x] Treat an empty changed-file list from CI as an error unless the job can prove it is an intentional no-op path. The existing validator's local no-op success must remain valid for hooks and direct CLI use.
  - [x] Keep PR validation on the real PR head commit and source branch; do not validate the synthetic merge commit message.

- [x] Task 2 - Make story-key extraction symmetric across CLI, trailers, and branch metadata (AC: 3)
  - [x] Update explicit `--story-key` parsing to use the same "all detected keys" behavior as trailer parsing, rejecting values with zero keys or more than one key.
  - [x] Update branch-name parsing to reject branch names containing more than one distinct story key instead of silently choosing the first match.
  - [x] Preserve existing precedence when all sources agree: CLI first, trailer second, branch third.
  - [x] Report every conflicting key and source in the diagnostic so contributors can fix the branch name, trailer, or CI input without guessing.

- [x] Task 3 - Wrap `git interpret-trailers` absence and trailer-parser failures cleanly (AC: 4)
  - [x] Catch `FileNotFoundError` around `git interpret-trailers --parse` and convert it to `ValidationError`.
  - [x] The message must say that Git with `interpret-trailers` is required and advise checking the Git installation or `PATH`.
  - [x] Keep non-zero `git interpret-trailers --parse` results as validation failures with stderr preserved, but without Python stack traces.

- [x] Task 4 - Strengthen parser and diagnostic boundary coverage (AC: 3, 4, 5)
  - [x] Add tests for multi-key `--story-key` values and multi-key branch names.
  - [x] Add `STORY_KEY_PATTERN` edge tests for a single-letter title segment, trailing hyphen rejection, uppercase input normalization, adjacent word/hyphen boundaries, and multiple-key reporting.
  - [x] Add parser tests for code fences longer than three backticks, nested fence-like content, bare-token bullet diagnostics, multiple `Allowed files for this story:` blocks, and termination only on known labels or headings.
  - [x] Add tests that assert fixture-based scope parsing reports the loaded story artifact path so loader-precedence regressions are visible.
  - [x] Harden test assertions so validation errors can move between stdout and stderr without creating brittle false failures.

- [x] Task 5 - Update contributor guidance and deferred-work bookkeeping (AC: 1, 2, 5, 6)
  - [x] Update `CONTRIBUTING.md` only where behavior changes: CI empty-diff/direct-push handling, multi-key branch/CLI rejection, and clean trailer-parser failure requirements.
  - [x] Mark resolved or remove target deferred entries in `_bmad-output/implementation-artifacts/deferred-work.md` with concise evidence after validation passes.
  - [x] Do not close target deferred entries that remain intentionally unimplemented; carry them forward with a fresh rationale and re-open trigger instead.

- [x] Task 6 - Validate the story-scope lane (AC: 1-6)
  - [x] Run `python tools/check-story-file-scope.py --help`.
  - [x] Run `python -m unittest discover -s tests/tooling/story_scope -p "*_test.py"`.
  - [x] Run focused manual validator probes for in-scope, out-of-scope, multi-key CLI, multi-key branch, empty CI changed-file behavior, and missing `git interpret-trailers` where feasible.
  - [x] If `.github/workflows/ci.yml` changes are non-trivial, use `git diff --check -- .github/workflows/ci.yml tools/check-story-file-scope.py tests/tooling/story_scope/story_scope_validator_test.py CONTRIBUTING.md _bmad-output/implementation-artifacts/deferred-work.md`.

### Review Findings

- [x] [Review][Patch] Bash PID interpolation uses invalid `${$}` expansion [.github/workflows/ci.yml:138]
- [x] [Review][Patch] Push-to-main/direct-push enforcement is unreachable while `main` pushes are ignored [.github/workflows/ci.yml:7]
- [x] [Review][Patch] Missing `git interpret-trailers` subcommand does not get the required install/PATH guidance [tools/check-story-file-scope.py:165]
- [x] [Review][Patch] Fence closing accepts same-length markers with trailing non-space text [tools/check-story-file-scope.py:278]
- [x] [Review][Patch] Hyphen-separated multi-key branch names are parsed as one key instead of reporting all keys [tools/check-story-file-scope.py:218]

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
- 2026-05-04: Implementation completed. CI hardening landed in `.github/workflows/ci.yml`: dropped `|| true` on `git fetch origin main`, dropped the `git diff-tree -r HEAD` fallback, switched to a 2-dot diff against an explicit `base_sha=$(git rev-parse origin/main)` on top of the existing `fetch-depth: 0` clone, hard-fail when `origin/main == HEAD` (direct-push / empty-diff), hard-fail when the changed-file list is empty in CI, hard-fail when `branch_name` / `head_sha` / `base_sha` are empty, and randomized the `BRANCH_NAME` heredoc delimiter via `date +%s%N`/`$$`/`$RANDOM`. PR-head validation continues to use `github.event.pull_request.head.sha` and `github.head_ref`; the synthetic merge commit is not validated.
- Validator hardening landed in `tools/check-story-file-scope.py`: `--story-key` rejects multi-key values and reports every detected key (12.4-RV7); branch-name parsing rejects branch values whose tokens, separated by non-`[\w-]` characters such as `/`, expose more than one distinct key (12.4-RV8); `subprocess.run(["git", ...])` calls in `parse_trailers` and `run_git` wrap `FileNotFoundError` and raise a clean `ValidationError` naming `git interpret-trailers` with an install/`PATH` hint (12.4-RV14); `parse_allowed_scope` now tracks the open code-fence character class and length so fences > 3 backticks containing nested 3-backtick fences and tilde fences containing nested backtick fences both parse correctly (12.4-RV12); allow-list collection terminates only on known section labels (`Read/verify only:`, `Forbidden by default:`, including `**bold:**` variants) or `## ` headings, so bullets whose rationale ends with `:` or unrelated trailing-colon prose do not silently truncate the allow-list (12.4-RV13); multiple `Allowed files for this story:` blocks now merge their entries consistently (12.3-RV15).
- Tests in `tests/tooling/story_scope/story_scope_validator_test.py`: added 14 focused regressions plus hardened the `section_block` helper to ignore blank lines (12.4-RV15) and added a `stdio()` helper that lets assertions match against combined stdout+stderr (12.4-RV17). New tests cover `STORY_KEY_PATTERN` boundaries (trailing-hyphen rejection, uppercase normalization, single-letter title segment), multi-key CLI/branch rejection, conflict-diagnostic source enumeration, missing `git interpret-trailers`, fences > 3 backticks, tilde fences with nested backtick fences, allow-list termination on known labels only (with three sub-cases for bullet trailing colon, prose trailing colon, and known-label termination), multiple `Allowed files for this story:` blocks, and pinning the `Story artifact:` line under fixture artifacts. Existing `test_branch_and_trailer_agreement_passes` now also asserts the diagnostic does NOT contain `Conflicting story keys`. Total focused suite: 39/39 PASS (was 25/25).
- CONTRIBUTING.md updated only where behavior changed: documented the multi-key rejection across `--story-key`, `Story:` / `Story-Key:` trailers, and branch names with `source=key` reporting; documented the clean `git interpret-trailers` ValidationError; documented the CI/local empty-diff split (CI fails loudly, local hooks/CLI keep the no-op success path); documented that fetch failures name the failed operation rather than degrading to a "every file in HEAD" fallback.
- Deferred-work bookkeeping: added `## Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)` section in `_bmad-output/implementation-artifacts/deferred-work.md` with one-line evidence per closed ID, and appended `[resolved in 14.1]` markers to the original 12.3-RV15 and 12.4-RV1, RV2, RV3, RV4, RV5, RV7, RV8, RV9, RV12, RV13, RV14, RV15, RV16, RV17, RV18 entries. Carried forward without closure: 12.4-RV6, RV10, RV11, RV19, RV20 — each with refreshed rationale and a re-open trigger.
- Validation evidence (2026-05-04): `python tools/check-story-file-scope.py --help` runs cleanly. `python -m unittest discover -s tests/tooling/story_scope -p "*_test.py"` is 39/39 green. Manual probes 1–8 all match expectations: in-scope file passes, out-of-scope file fails, multi-key `--story-key` rejects with both keys reported, multi-key `/`-separated branch rejects with both keys reported, missing `git` (PATH unset) emits the new clean `Required tool not found: 'git interpret-trailers' is unavailable. Install Git ...` message and rc=1 (no Python traceback), local CLI no-op-success path is preserved (rc=0 with explicit "No changed files" message), `Scope-Override` authorizes a non-forbidden path, `Scope-Override` does NOT authorize a forbidden-default `src/**/*.cs` file. `git diff --check -- .github/workflows/ci.yml tools/check-story-file-scope.py tests/tooling/story_scope/story_scope_validator_test.py CONTRIBUTING.md _bmad-output/implementation-artifacts/deferred-work.md` returns rc=0. Validator self-check against the actual working-tree diff with `--story-key 14-1-ci-story-scope-enforcement-hardening` confirms every changed file is in the story's allow-list.
- 2026-05-04 code review close-out: 5/5 patch findings applied. Fixed invalid bash PID expansion in the randomized heredoc delimiter; enabled `push` coverage for `main` and routed `main` pushes through the loud origin/main equality guard; added install/PATH guidance for non-zero `git interpret-trailers --parse` failures; required closing fences to have only trailing whitespace after the marker; and detected `-and-`-joined multi-key branch names. Added 3 focused regressions plus one direct parser unit for the subcommand failure path. Validation after patches: `python tools/check-story-file-scope.py --help` clean, `python -m unittest discover -s tests/tooling/story_scope -p "*_test.py"` 42/42 green, actual working-tree story-scope self-check passed, and `git diff --check` clean.

### File List

- `.github/workflows/ci.yml` - UPDATED: `story-file-scope` job hardened (drop `|| true` on fetch, drop `git diff-tree -r HEAD` fallback, 2-dot diff, direct-push detection, empty-diff hard fail, missing-env hard fail, randomized heredoc delimiter, named `::error::` diagnostics).
- `tools/check-story-file-scope.py` - UPDATED: `STORY_KEY_PATTERN` matching boundary tests (helper changes only); `CODE_FENCE_PATTERN` widened to `^\s*(\`{3,}|~{3,})`; `TERMINATING_LABELS` introduced; `parse_allowed_scope` rewritten to track fence marker character/length and terminate only on known labels/headings; `resolve_story_key` now rejects multi-key `--story-key` and multi-key branch values with all keys reported; `parse_trailers` and `run_git` wrap `FileNotFoundError` into clean `ValidationError`s.
- `tests/tooling/story_scope/story_scope_validator_test.py` - UPDATED: hardened `section_block` (no blank-line termination), added `stdio()` combined-sink helper, hardened `test_unparseable_explicit_story_key_fails_closed` and `test_branch_and_trailer_agreement_passes`, added 14 new focused regressions covering all 14.1 acceptance criteria boundary cases.
- `CONTRIBUTING.md` - UPDATED: documented multi-key rejection with source enumeration, clean trailer-parser failure behavior, and CI/local empty-diff split.
- `_bmad-output/implementation-artifacts/deferred-work.md` - UPDATED: added `## Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)` evidence section; appended `[resolved in 14.1]` markers to the 16 closed entries (12.3-RV15 and 15 of the 12.4-RV* entries); carried forward 12.4-RV6, RV10, RV11, RV19, RV20 with refreshed rationale and re-open triggers.
- `_bmad-output/implementation-artifacts/14-1-ci-story-scope-enforcement-hardening.md` - UPDATED: implementation notes, validation evidence, review findings, file list, change log; status moved ready-for-dev → in-progress → review → done.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATED through BMad workflow: status transitions ready-for-dev → in-progress → review → done and refreshed `last_updated`.

### Change Log

- 2026-05-03: Created Story 14.1 and promoted it from `backlog` to `ready-for-dev`.
- 2026-05-03: Party-mode review completed; constraints triaged and recorded for development.
- 2026-05-04: Implementation complete. Hardened CI `story-file-scope` job (12.4-RV1..RV5), made story-key extraction symmetric across CLI/trailer/branch (12.4-RV7, RV8), wrapped `git interpret-trailers` absence cleanly (12.4-RV14), strengthened parser termination and code-fence handling (12.4-RV12, RV13), expanded validator boundary tests and hardened test helpers (12.4-RV9, RV15..RV18, plus 12.3-RV15), updated CONTRIBUTING.md only where behavior changes, recorded all closures in deferred-work.md, and validated the story-scope lane (39/39 focused tests + 8 manual probes + `git diff --check` clean). Status moved ready-for-dev → in-progress → review.
- 2026-05-04: Code-review patches complete. Applied all 5 patch findings and revalidated the lane (42/42 focused tests, actual working-tree story-scope self-check, `git diff --check`). Status moved review → done.

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

Story 14.1 implementation and code-review patch close-out complete. Status set to `done`.
