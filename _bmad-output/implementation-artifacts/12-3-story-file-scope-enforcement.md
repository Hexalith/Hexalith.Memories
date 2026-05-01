# Story 12.3: Story-File-Scope Enforcement

Status: ready-for-dev

**Effort estimate:** ~1.5-2.0 working days.

- **0.25 day - Task 0:** inspect the current story-file `File Scope` shapes and lock one parser contract against the stories already in `_bmad-output/implementation-artifacts/`.
- **0.50 day - Task 1:** implement a single cross-platform validator that resolves the story key, reads the story artifact, extracts the allowed/forbidden scope, and compares it against a diff file list plus optional `Scope-Override:` trailers.
- **0.35 day - Task 2:** wire local Git hooks for fast feedback without adding third-party hook tooling.
- **0.30 day - Task 3:** wire a required CI job so the guardrail cannot be bypassed by skipping local hooks.
- **0.20 day - Task 4:** update contributor documentation and validate the end-to-end operator flow.

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a sprint discipline owner,
I want diffs to be checked against the originating story's `File Scope` declaration,
so that the D5-style file-scope leak from Epic 11 cannot recur silently.

## Acceptance Criteria

1. Given every story file in `_bmad-output/implementation-artifacts/` has a `File Scope` section listing the file globs it is allowed to touch, when a developer prepares a commit that references a story via branch name, commit trailer, or explicit annotation, then an automated check compares the staged diff against that story's `File Scope` and fails loudly when files outside the declared scope are touched.

2. Given legitimate cross-story stabilization sometimes requires touching files the story did not anticipate, when a developer needs an explicit override, then they can include a `Scope-Override:` line in the commit message naming the affected file(s) and a short rationale, and the check passes only when the override covers every out-of-scope file.

3. Given the check is in place, when Epic 11's D5-style scenario recurs and a CI or release story touches `src/**/*.cs`, then the check fires at commit or PR time rather than waiting for adversarial review.

4. Given the check exists, then `CONTRIBUTING.md` documents how to declare `File Scope` correctly, how the story key is discovered, how to install the local hooks, and how to use `Scope-Override:` legitimately.

## Tasks / Subtasks

- [ ] Task 0 - Lock the parser contract against real story files (AC: 1, 2)
  - [ ] Read several current story artifacts with `## File Scope` sections (`11-1`, `11-2`, `12-1`, `12-2`, `13-1`) and define the exact parser rules for:
    - `Allowed files for this story:`
    - `Read/verify only:`
    - `Forbidden by default:`
    - the free-form explanatory line that follows those lists
  - [ ] Treat the `Allowed files for this story:` list as the authoritative writable allow-list.
  - [ ] Treat `Read/verify only:` and `Forbidden by default:` as explanatory validation output and reviewer guidance, not as the primary matching source.
  - [ ] Fail with a clear error if the selected story file has no parseable `File Scope` section.

- [ ] Task 1 - Build one shared story-scope validator (AC: 1, 2, 3)
  - [ ] Add a single cross-platform script, preferably `tools/check-story-file-scope.py`, as the only place that:
    - resolves the target story key
    - finds the matching story artifact
    - parses allowed globs from `## File Scope`
    - collects changed paths from either staged diff input or a caller-provided file list
    - parses commit trailers for `Story:` / `Story-Key:` and `Scope-Override:`
    - emits a non-zero exit code plus human-readable diagnostics on mismatch
  - [ ] Resolve the story key in this precedence order:
    - explicit `Story:` or `Story-Key:` trailer in the commit message
    - branch name containing a full story key such as `12-3-story-file-scope-enforcement`
    - explicit CLI argument from CI or the hook wrapper
  - [ ] Parse commit trailers with `git interpret-trailers --parse` instead of ad hoc footer splitting.
  - [ ] Treat `Scope-Override:` as a trailer, not a prose sentence. Require it to name the out-of-scope file or glob plus a short rationale.
  - [ ] Require every changed file to match at least one allowed glob unless a matching `Scope-Override:` covers it.
  - [ ] Print the selected story key, the story artifact path, the offending file list, and the specific unmatched files in failure output.

- [ ] Task 2 - Add local Git-hook enforcement without introducing new package tooling (AC: 1, 2, 4)
  - [ ] Add repo-managed hook files under a tracked hooks directory such as `.githooks/`.
  - [ ] Add a `pre-commit` hook that checks the staged diff using `git diff --cached --name-only --`.
  - [ ] Add a `commit-msg` hook that reads the proposed commit message file and validates trailer-based `Story:` / `Story-Key:` / `Scope-Override:` semantics.
  - [ ] Use `core.hooksPath` documentation and/or a lightweight setup command in `CONTRIBUTING.md`; do not introduce Husky or another Node-only hook framework just for this story.
  - [ ] Keep the hook wrappers thin. The Python validator should contain the logic so local hooks and CI cannot drift.

- [ ] Task 3 - Add CI enforcement for branch/PR review time (AC: 1, 2, 3)
  - [ ] Update `.github/workflows/ci.yml` with a dedicated required job, e.g. `story-file-scope`, that runs on the same `pull_request` and non-`main` `push` triggers as the existing CI workflow.
  - [ ] In PR context, derive the source branch from `github.head_ref` and the authoritative head commit message from `github.event.pull_request.head.sha` rather than the synthetic merge commit.
  - [ ] In push context, validate the checked-out branch and `HEAD` commit message.
  - [ ] Keep the CI job narrowly scoped to this guardrail. It must not rebuild the .NET solution or run Docker-backed tests.
  - [ ] Make the new job fail loudly when a story-key cannot be resolved, a story artifact is missing, a `File Scope` section cannot be parsed, or changed files fall outside the allowed scope without an override.

- [ ] Task 4 - Add focused automated coverage and fixtures (AC: 1, 2, 3)
  - [ ] Add focused regression coverage for the validator itself. Prefer stdlib-based Python tests or script fixtures over introducing a new test dependency.
  - [ ] Cover at minimum:
    - branch-name story-key discovery
    - `Story:` trailer discovery
    - `Scope-Override:` trailer parsing
    - in-scope diff passes
    - out-of-scope diff fails
    - out-of-scope diff plus matching override passes
    - D5-style `src/**/*.cs` touch under a doc/CI story fails
  - [ ] Add at least one fixture using a real current story file pattern so parser drift in future story templates is caught quickly.

- [ ] Task 5 - Document and validate the operator flow (AC: 4)
  - [ ] Update `CONTRIBUTING.md` with:
    - how to write `## File Scope`
    - how story discovery works
    - how to install repo-managed hooks
    - when `Scope-Override:` is legitimate and how specific it must be
  - [ ] Validate the happy path and failure path with explicit commands and captured output.
  - [ ] Confirm the new job can become a required PR check without depending on runtime source changes, release tooling changes, or submodule edits.

## File Scope

Allowed files for this story:

- `tools/check-story-file-scope.py` - NEW. Canonical validator used by hooks and CI.
- `.githooks/pre-commit` - NEW. Thin wrapper for staged-diff validation.
- `.githooks/commit-msg` - NEW. Thin wrapper for trailer validation against the proposed commit message file.
- `.github/workflows/ci.yml` - UPDATE. Add the required `story-file-scope` job.
- `CONTRIBUTING.md` - UPDATE. Document File Scope authoring, hook setup, and `Scope-Override:` usage.
- `tests/tooling/story_scope/**/*.py` - NEW. Focused validator regression tests and fixtures, if stdlib `unittest` is used.
- `_bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md` - UPDATE Dev Agent Record and completion notes.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through the BMad workflow when story state changes.

Read/verify only:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-26.md`
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md`
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-30.md`
- `_bmad-output/implementation-artifacts/11-1-github-actions-build-and-test-pipeline.md`
- `_bmad-output/implementation-artifacts/11-2-semantic-release-and-nuget-publishing.md`
- `_bmad-output/implementation-artifacts/12-1-first-release-path-validation.md`
- `_bmad-output/implementation-artifacts/12-2-forbidden-default-tolerances-checklist.md`
- `package.json`
- `.github/workflows/release.yml`
- `tools/test.ps1`
- `tools/test.sh`
- `tools/verify-integration-fast-coverage.py`

Forbidden by default:

- `src/**/*.cs`
- `tests/**/*.cs`
- `tools/publish-nuget.ps1`
- `tools/pack-release.ps1`
- `tools/test-release.ps1`
- `package-lock.json`
- submodule contents, including `Hexalith.AI.Tools`, `Hexalith.Commons`, and `Hexalith.EventStore`

If implementation discovers story files whose `File Scope` sections are too inconsistent to parse safely, do not widen this story into a bulk artifact-normalization pass. Fix only the minimum parser-compatible cases needed for active Epic 12 / Epic 13 stories, and capture broader normalization as a follow-up if necessary.

## Dev Notes

### Epic Context

Epic 12 is the post-MVP operations hardening pass that converts Epic 11 retrospective lessons into enforceable guardrails. Story 12.3 operationalizes Epic 11 Action A4: story file scope is a contract, not a hint.

This story exists because Story 11.2 leaked runtime `.cs` changes under a CI/release-scoped diff. The retrospective recorded the leak as D5 and explicitly called out the absence of tooling that could reject `src/**/*.cs` changes before review. This story is the guardrail that should have caught that class of error immediately.

Do not broaden this story into:

- general release hardening
- partial-publish alerting
- baseline failure cleanup
- runtime stabilization
- broad story-artifact rewrites

Those are owned by Stories 12.4, 12.5, or later follow-ups.

### Current Repository Shape

Relevant repo facts at story creation time:

- `package.json` exists only for commitlint and semantic-release tooling. There is no existing Husky or hook manager setup.
- `.github/workflows/ci.yml` already defines required jobs `build`, `test-unit-contract`, and `integration-fast`. Additive CI enforcement should fit this pattern rather than inventing a second workflow.
- `tools/test.sh` and `tools/test.ps1` already show the repository preference for thin shell wrappers around shared logic and explicit failure on zero-result states.
- Current story artifacts already include `## File Scope` sections, but they are human-authored Markdown, not a machine-readable schema. The parser must therefore be strict enough to prevent false passes while tolerant enough to handle the current list formatting.

### Recommended Implementation Shape

The safest implementation is one validator with multiple entrypoints:

1. `tools/check-story-file-scope.py`
   - accepts explicit arguments for changed-file input, branch name, commit-message file or commit SHA, and optional forced story key
   - owns all parsing and matching logic
2. `.githooks/pre-commit`
   - gathers staged paths with `git diff --cached --name-only --`
   - invokes the Python validator
3. `.githooks/commit-msg`
   - passes the proposed commit message file to the same validator
4. `.github/workflows/ci.yml`
   - invokes the same validator against PR/push diffs so the guardrail remains enforced even when local hooks are skipped

Avoid split implementations in PowerShell plus Bash plus YAML expressions. Drift between three implementations would recreate the exact governance weakness this story is trying to remove.

### Story-Key and Override Semantics

Make the resolution rules explicit in code and docs:

- A full story key means the sprint-status form, e.g. `12-3-story-file-scope-enforcement`.
- `Story:` or `Story-Key:` should be treated as commit trailers, not free-form prose.
- `Scope-Override:` should also be treated as a trailer and should be specific enough for retrospective audit.
- A missing story key should fail the guardrail unless the caller passed one explicitly.
- A broad override like `Scope-Override: *` or `Scope-Override: repo-wide cleanup` should be rejected as too vague.

Recommended audit-friendly shape:

```text
Scope-Override: src/Hexalith.Memories.Server/Search/SemanticSearchService.cs - fix pre-existing D5-class runtime defect discovered while stabilizing CI lane
```

The implementation may allow multiple `Scope-Override:` trailers when more than one path needs explicit justification.

### D5 Regression to Prove

The must-not-miss regression case is a CI/release/documentation story whose allow-list does not include `src/**/*.cs`, but whose diff touches runtime `.cs` anyway. The validator must fail clearly enough that a contributor understands:

- which story was selected
- which file scope was loaded
- which changed files were allowed
- which changed files were forbidden
- whether a `Scope-Override:` trailer was present and why it did not match

If this case passes silently, the story failed its main purpose.

### Project Context Reference

The BMad persistent-facts glob resolved a `project-context.md` file inside `Hexalith.Commons`, not at the `Hexalith.Memories` repository root. The directly relevant carry-forward rules for this story are:

- use conventional commits
- do not modify shared submodule contents without explicit need
- keep changes narrow and intentional
- preserve centralized tooling patterns rather than adding one-off package dependencies

Because that file belongs to a sibling/submodule repository, treat it as background convention only. The story-specific scope and constraints in this artifact take precedence for implementation.

### Latest Technical Information

Web verification performed on 2026-05-01 using primary sources:

- Git's current `githooks` documentation says hooks run from `$GIT_DIR/hooks` by default and can be relocated with `core.hooksPath`. It also states that `pre-commit` and `commit-msg` abort the commit on non-zero exit, which makes them the right local enforcement points. Source: https://git-scm.com/docs/githooks
- Git's current `git diff` documentation defines `git diff --cached` as the staged-diff view for the next commit and supports restricting the output to paths. That is the right primitive for a pre-commit scope check. Source: https://git-scm.com/docs/git-diff
- Git's current `git interpret-trailers` documentation says trailer lines at the end of commit messages can be parsed with `--parse`. Use this instead of hand-rolled footer parsing for `Story:` and `Scope-Override:`. Source: https://git-scm.com/docs/git-interpret-trailers
- GitHub Actions current docs state that `github.head_ref` is the source branch for pull-request workflows and that PR workflows should use `github.event.pull_request.head.sha` when they need the real head commit rather than the synthetic merge SHA. Source: https://docs.github.com/en/actions/reference/workflows-and-actions/contexts and https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows

### Testing Requirements

Minimum validation before review:

```powershell
python tools/check-story-file-scope.py --help
python -m unittest discover -s tests/tooling/story_scope -p \"*_test.py\"
git diff --cached --name-only --
```

Minimum behavior to demonstrate in completion notes:

- in-scope change passes without override
- out-of-scope change fails without override
- out-of-scope change passes with a matching `Scope-Override:` trailer
- PR/CI job uses the source branch and head commit rather than the merge ref defaults

Do not run a full .NET build unless the implementation unexpectedly touches runtime code or existing CI/test scripts beyond the dedicated guardrail job.

## References

- `_bmad-output/planning-artifacts/epics.md` - Story 12.3 acceptance criteria.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-26.md` - Option C and the initial A4 implementation sketch.
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md` - D5 incident and the original A4 action item.
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-30.md` - refreshed A4 carry-forward guidance and "story file scope is a contract" team agreement.
- `_bmad-output/implementation-artifacts/11-1-github-actions-build-and-test-pipeline.md` - current CI shape and stable required checks.
- `_bmad-output/implementation-artifacts/11-2-semantic-release-and-nuget-publishing.md` - the concrete story whose D5 leak motivated this guardrail.
- `_bmad-output/implementation-artifacts/12-1-first-release-path-validation.md` - Epic 12 scope discipline and `Scope-Override:` wording precedent.
- `_bmad-output/implementation-artifacts/12-2-forbidden-default-tolerances-checklist.md` - sibling governance-story file-scope pattern.
- `package.json` - confirms commitlint/semantic-release exist but no hook manager exists.
- `.github/workflows/ci.yml` - existing CI trigger model and required job names.
- `.github/workflows/release.yml` - read-only comparison point; this story should not alter release behavior.
- `tools/test.ps1`, `tools/test.sh`, `tools/verify-integration-fast-coverage.py` - current thin-wrapper tooling style to mirror.
- Git hooks docs: https://git-scm.com/docs/githooks
- Git diff docs: https://git-scm.com/docs/git-diff
- Git interpret-trailers docs: https://git-scm.com/docs/git-interpret-trailers
- GitHub Actions contexts docs: https://docs.github.com/en/actions/reference/workflows-and-actions/contexts
- GitHub Actions events docs: https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Pre-dev hardening preflight passed on 2026-05-01 and bootstrapped `_bmad-output/process-notes/story-creation-lessons.md`.
- Story selection logic chose `12-3-story-file-scope-enforcement` because the ready-for-dev buffer was `0`, below the target of `5`, and this was the first backlog story in sprint-status order.

### Completion Notes List

- Story context created on 2026-05-01.
- Discovery loaded the relevant Epic 12 planning material and Epic 11 retrospective findings that introduced Action A4.
- No `Hexalith.Memories` root `project-context.md` file was present; a sibling `Hexalith.Commons` context file existed and was used only for background conventions.
- The recommended implementation keeps one canonical validator and reuses it from hooks plus CI to avoid logic drift.
- The story intentionally avoids Husky or other new package tooling because the repo already has a lightweight script-based tooling pattern and no existing hook manager.

### File List

- `_bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-01: Created Story 12.3 and promoted it from `backlog` to `ready-for-dev`.

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created. Status set to `ready-for-dev`.
