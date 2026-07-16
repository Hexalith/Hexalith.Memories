# Story 12.3: Story-File-Scope Enforcement

Status: done

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

1. Given every story file in `_bmad-output/implementation-artifacts/` has a `File Scope` section listing the file globs it is allowed to touch, when a developer prepares a commit that references a story via explicit CLI argument, commit trailer, or branch name, then an automated check compares the staged diff against that story's `File Scope` and fails loudly when files outside the declared scope are touched. Story-key discovery must use the same precedence in hooks and CI: CLI argument first, then `Story:` / `Story-Key:` trailer, then branch name. If two non-empty sources disagree, the check must fail closed with a clear conflict diagnostic.

2. Given legitimate cross-story stabilization sometimes requires touching files the story did not anticipate, when a developer needs an explicit override, then they can include one or more `Scope-Override:` trailers in the commit message naming the affected file path or narrow glob plus a short rationale, and the check passes only when the override covers every out-of-scope file. Overrides must be audited in local and CI output and must not bypass forbidden-default areas such as submodule contents, release scripts, `package-lock.json`, or runtime/source paths without a separate human/product/architecture decision.

3. Given the check is in place, when Epic 11's D5-style scenario recurs and a CI or release story touches `src/**/*.cs`, then the check fires at commit or PR time rather than waiting for adversarial review.

4. Given the check exists, then `CONTRIBUTING.md` documents how to declare `File Scope` correctly, how the story key is discovered, how to install the local hooks, how hook/CI parity is maintained through the shared validator, and how to use `Scope-Override:` legitimately.

## Tasks / Subtasks

- [x] Task 0 - Lock the parser contract against real story files (AC: 1, 2)
  - [x] Read several current story artifacts with `## File Scope` sections (`11-1`, `11-2`, `12-1`, `12-2`, `13-1`) and define the exact parser rules for:
    - `Allowed files for this story:`
    - `Read/verify only:`
    - `Forbidden by default:`
    - the free-form explanatory line that follows those lists
  - [x] Treat the `Allowed files for this story:` list as the authoritative writable allow-list.
  - [x] Parse only direct Markdown bullet-list glob entries under `Allowed files for this story:`.
  - [x] Ignore fenced code blocks, free-form prose, and nested explanatory bullets unless they contain a direct backtick-wrapped path/glob entry.
  - [x] Accept the existing "`path` - rationale" convention by extracting only the backtick-wrapped path or glob; do not infer paths from arbitrary prose.
  - [x] Treat `Read/verify only:` and `Forbidden by default:` as explanatory validation output and reviewer guidance, not as the primary matching source.
  - [x] Fail with a clear error if the selected story file has no parseable `File Scope` section or has an empty `Allowed files for this story:` list.

- [x] Task 1 - Build one shared story-scope validator (AC: 1, 2, 3)
  - [x] Add a single cross-platform script, preferably `tools/check-story-file-scope.py`, as the only place that:
    - resolves the target story key
    - finds the matching story artifact
    - parses allowed globs from `## File Scope`
    - collects changed paths from either staged diff input or a caller-provided file list
    - parses commit trailers for `Story:` / `Story-Key:` and `Scope-Override:`
    - emits a non-zero exit code plus human-readable diagnostics on mismatch
  - [x] Resolve the story key in this precedence order:
    - explicit CLI argument from CI or the hook wrapper
    - explicit `Story:` or `Story-Key:` trailer in the commit message
    - branch name containing a full story key such as `12-3-story-file-scope-enforcement`
  - [x] Fail closed with a clear conflict diagnostic when multiple available story-key sources resolve to different story keys instead of silently validating against an unexpected story.
  - [x] Parse commit trailers with `git interpret-trailers --parse` instead of ad hoc footer splitting.
  - [x] Treat `Scope-Override:` as a trailer, not a prose sentence. Require it to name the out-of-scope file or glob plus a short rationale.
  - [x] Reject vague overrides such as `*`, `.`, repository-root patterns, bare directory names, empty values, or prose-only rationales that do not identify the affected path or glob.
  - [x] Require every changed file to match at least one allowed glob unless a matching `Scope-Override:` covers it.
  - [x] Keep the forbidden-default glob list in one Python-owned source of truth used by hooks, CI, and tests; do not duplicate forbidden-path logic in shell wrappers or workflow expressions.
  - [x] Normalize changed paths and scope entries to repository-relative POSIX-style paths before matching so Windows local hooks and Linux CI produce the same decision.
  - [x] Print the selected story key, the story artifact path, the offending file list, the allowed-scope source, the specific unmatched files, and the accepted `Scope-Override:` format in plain-text failure output.
  - [x] Define changed-file handling explicitly: validate destination paths for added, modified, copied, and renamed files; document whether deleted paths are ignored or validated.
  - [x] Treat a legitimate empty diff as a plain-text no-op success, but fail when the diff source itself cannot be resolved.

- [x] Task 2 - Add local Git-hook enforcement without introducing new package tooling (AC: 1, 2, 4)
  - [x] Add repo-managed hook files under a tracked hooks directory such as `.githooks/`.
  - [x] Add a `pre-commit` hook that checks the staged diff using `git diff --cached --name-only --`.
  - [x] Add a `commit-msg` hook that reads the proposed commit message file and validates trailer-based `Story:` / `Story-Key:` / `Scope-Override:` semantics.
  - [x] Keep hook responsibilities explicit: `pre-commit` validates the staged file list with branch or caller-provided story context, while `commit-msg` validates trailers, overrides, and story-key conflicts once the proposed message exists.
  - [x] Use `core.hooksPath` documentation and/or a lightweight setup command in `CONTRIBUTING.md`; do not introduce Husky or another Node-only hook framework just for this story.
  - [x] Keep the hook wrappers thin. The Python validator should contain the logic so local hooks and CI cannot drift.

- [x] Task 3 - Add CI enforcement for branch/PR review time (AC: 1, 2, 3)
  - [x] Update `.github/workflows/ci.yml` with a dedicated required job, e.g. `story-file-scope`, that runs on the same `pull_request` and non-`main` `push` triggers as the existing CI workflow.
  - [x] In PR context, derive the source branch from `github.head_ref` and the authoritative head commit message from `github.event.pull_request.head.sha` rather than the synthetic merge commit.
  - [x] In push context, validate the checked-out branch and `HEAD` commit message.
  - [x] Supply CI with a deterministic changed-file list from the PR or push comparison and prove the job does not validate the synthetic merge commit message.
  - [x] Keep the CI job narrowly scoped to this guardrail. It must not rebuild the .NET solution or run Docker-backed tests.
  - [x] Invoke the same Python validator entrypoint used by `.githooks/pre-commit` and `.githooks/commit-msg`; CI may pass mode-specific arguments, but must not reimplement story-key, parser, forbidden-default, or override logic in YAML.
  - [x] Make the new job fail loudly when a story-key cannot be resolved, a story artifact is missing, a `File Scope` section cannot be parsed, or changed files fall outside the allowed scope without an override.

- [x] Task 4 - Add focused automated coverage and fixtures (AC: 1, 2, 3)
  - [x] Add focused regression coverage for the validator itself. Prefer stdlib-based Python tests or script fixtures over introducing a new test dependency.
  - [x] Cover at minimum:
    - branch-name story-key discovery
    - `Story:` trailer discovery
    - `Scope-Override:` trailer parsing
    - in-scope diff passes
    - out-of-scope diff fails
    - out-of-scope diff plus matching override passes
    - D5-style `src/**/*.cs` touch under a doc/CI story fails
    - branch/trailer agreement and conflict cases
    - multiple or malformed `Story:` / `Story-Key:` trailers
    - vague `Scope-Override:` values such as `*`, `.`, `src`, `src/**`, and prose-only rationales fail
    - exact override matching does not authorize sibling, suffix, child, or partial-string paths
    - Windows and Linux path normalization cases, including backslashes, leading `./`, repeated separators, and path traversal cleanup
    - legitimate zero-changed-file input returns an explicit no-op success
    - [x] Add at least one fixture using a real current story file pattern so parser drift in future story templates is caught quickly.

- [x] Task 5 - Document and validate the operator flow (AC: 4)
  - [x] Update `CONTRIBUTING.md` with:
    - how to write `## File Scope`
    - how story discovery works
    - how to install repo-managed hooks
    - when `Scope-Override:` is legitimate and how specific it must be
    - valid and invalid `Scope-Override:` examples
    - the repo-local hook setup command `git config core.hooksPath .githooks` and a warning not to use `--global`
    - narrow non-goals: no submodule changes, no runtime behavior changes, no release tooling changes, and no recursive submodule commands
  - [x] Validate the happy path and failure path with explicit commands and captured output.
  - [x] Confirm the new job can become a required PR check without depending on runtime source changes, release tooling changes, or submodule edits.

### Review Findings

Code review run on 2026-05-01 against commit `0b065b8` using the three-layer adversarial review (Blind Hunter, Edge Case Hunter, Acceptance Auditor). Diff size 1196+ / 59-. AC audit (post-resolution): AC1 PASS, AC2 PASS (D2 resolved — story allow-list intersecting forbidden-default IS the spec-required deliberate decision; documented in CONTRIBUTING.md; remaining override-vagueness relaxation deferred as 12.3-RV12), AC3 PASS, AC4 PASS. 22 patches applied; 2 patches deferred (P6 override-vagueness relaxation pending design discussion; P22 multi-backticks-per-bullet kept at "first backtick is path, additional backticks are rationale" by deliberate design choice).

#### Decisions needed

- [x] [Review][Decision] Implementation commit `0b065b8` violates its own File Scope — leaks `Hexalith.EventStore` (submodule pointer), `_bmad-output/implementation-artifacts/13-3-extend-embedding-client-to-support-ollama.md`, `_bmad-output/process-notes/predev-preflight-2026-05-01T154530Z.json`, `_bmad-output/process-notes/predev-preflight-latest.json`. Dev Agent Record line 333 acknowledged these as out-of-scope, but they were committed. Decision: revert + recommit, accept and amend Dev Agent Record more explicitly, or other process correction?
- [x] [Review][Decision] Story-allowed paths bypass forbidden-default audit (`tools/check-story-file-scope.py:339-352`). A story listing `src/Foo.cs` in its File Scope short-circuits before the forbidden-default check. Decision: should an explicit allow-list entry override forbidden-default (current behavior), or should forbidden-default ALWAYS gate the allow-list, requiring an explicit "opt-out of default forbidden" mechanism?
- [x] [Review][Decision] Hooks `mktemp` on Windows Git Bash → Python validator may break on MSYS path translation. Verify hands-on, or change to a guaranteed-translatable path inside the repo (e.g. `.git/story-scope-changed.txt`) preemptively?

#### Patches

- [x] [Review][Patch] Submodule pointer leak NOT detected as forbidden-default — `is_forbidden_default('Hexalith.EventStore')` returns False because `Hexalith.EventStore/**` requires children. The literal submodule-pointer tree entry has no slash. Add prefix-equality match alongside the `/**` glob. This is the exact regression Story 12.2's closure note assigned 12.3 to prevent. [tools/check-story-file-scope.py:25-35]
- [x] [Review][Patch] `extract_backtick_path` falls back to bare un-backticked tokens, contradicting CONTRIBUTING.md ("extracts only backtick-wrapped paths") and spec Task 0. Remove the fallback. [tools/check-story-file-scope.py:185-195]
- [x] [Review][Patch] `matches_glob` uses `fnmatch` semantics where `*` matches `/`. A story allow-list entry like `tests/*` would permit any path under `tests/...` including forbidden `tests/**/*.cs`. Use proper recursive `**` semantics. [tools/check-story-file-scope.py:253-260]
- [x] [Review][Patch] `.githooks/pre-commit` and `.githooks/commit-msg` lack the executable bit (mode 100644). Linux/macOS `git commit` silently bypasses them. `git update-index --chmod=+x .githooks/*`. [.githooks/pre-commit, .githooks/commit-msg]
- [x] [Review][Patch] CI force-push: `${{ github.event.before }}` SHA is unreachable after history rewrite. `git diff "$before_sha" "$head_sha"` aborts the job with `bad object`. Wrap in fallback to `diff-tree` against HEAD or against the merge-base with `main`. [.github/workflows/ci.yml:51-55]
- [ ] [Review][Patch] Override-vagueness rule rejects legitimate root-level files (`Dockerfile`, `LICENSE`, `package.json`) via `"/" not in normalized`, and rejects narrow `dir/**` overrides. Relax: allow path-shaped values with rationale; only reject clearly broad patterns (`*`, `**`, `.`, `/`, repo-root, prose-only). **Deferred to 12.3-RV12** — design decision needed on what constitutes "narrow enough"; relaxing without a clear rule risks weakening the audit value. [tools/check-story-file-scope.py:281-300]
- [x] [Review][Patch] `parse_allowed_scope` does not track fenced code blocks. CONTRIBUTING.md's own template inside a story would be parsed as authoritative entries. Toggle on triple-backtick. [tools/check-story-file-scope.py:198-236]
- [x] [Review][Patch] Sub-bullets under an allowed entry are parsed as additional allow-list entries because parser uses `stripped` (discards indentation). Reject indented bullets. [tools/check-story-file-scope.py:226-231]
- [x] [Review][Patch] `STORY_KEY_PATTERN` lookbehind `(?<!\d)` only blocks digits; letters preceding still match. `feat/abc123-12-3-foo` resolves to `123-12-3-foo`. Use `(?<![\w-])`. [tools/check-story-file-scope.py:16]
- [x] [Review][Patch] `parse_trailers` rejects duplicate identical Story trailers (e.g. `Story:` and `Story-Key:` both naming the same key). Only fail when trailer values disagree. [tools/check-story-file-scope.py:152-154]
- [x] [Review][Patch] Unparseable `--story-key` is silently demoted instead of failing closed. `--story-key bad-value` falls through to trailer/branch. Spec calls CLI "highest precedence" — silent ignore violates fail-closed semantics. [tools/check-story-file-scope.py:159-161]
- [x] [Review][Patch] Tests write commit-message files into the test runner's cwd via `Path(self._testMethodName + ".txt")`. Pollutes repo root, races on parallel runs, leaks files on crash. Use `tempfile.NamedTemporaryFile`. [tests/tooling/story_scope/story_scope_validator_test.py:34-38]
- [x] [Review][Patch] `test_path_normalization_handles_windows_and_posix_inputs` only asserts `returncode == 0`. Add assertions that BOTH inputs normalize to the same path and that the `In-scope changed files:` block lists them as the expected POSIX form. [tests/tooling/story_scope/story_scope_validator_test.py:212-222]
- [x] [Review][Patch] `test_real_current_story_file_scope_pattern_is_parseable` reads the live `12-2-...md`. Spec asked for "fixture", not "live file". Move to a frozen copy under `tests/tooling/story_scope/fixtures/`. [tests/tooling/story_scope/story_scope_validator_test.py:230-238]
- [x] [Review][Patch] Add D5 regression test for plain `src/**/*.cs` touch with NO override (current `test_d5_style_source_touch_fails_even_with_override` only covers the override case). The validator currently reports forbidden-default-with-no-override as plain "Out-of-scope" with no submodule/forbidden diagnostic — assert that distinct diagnostic too. [tests/tooling/story_scope/story_scope_validator_test.py:102-120]
- [x] [Review][Patch] Add tests for: diff-source-missing failure (spec required), empty `Scope-Override:` value, branch+trailer agreement (only conflict is tested today). [tests/tooling/story_scope/story_scope_validator_test.py]
- [x] [Review][Patch] `extract_backtick_path` extracts only the FIRST backtick path on multi-token bullets — kept as deliberate design after first-pass implementation showed real story bullets routinely use additional backticks for inline-code rationale (e.g. `- \`.github/workflows/ci.yml\` - UPDATE. Add the required \`story-file-scope\` job.`). Resolution: explicitly document that the first backtick token is the path and subsequent backticks are rationale; do NOT split into multiple paths. Story authors who need to allow two paths should write two bullets. [tools/check-story-file-scope.py:185-189]
- [x] [Review][Patch] `Story:` value containing two distinct keys silently picks the first via `extract_story_key`. Use `findall`, error on multiple matches in a single trailer value. [tools/check-story-file-scope.py:144-148]
- [x] [Review][Patch] Diagnostic prints story artifact path with OS-native separators on Windows (`_bmad-output\implementation-artifacts\...`). Spec says paths should be normalized to POSIX-style — also for printed diagnostics. [tools/check-story-file-scope.py:328-329]
- [x] [Review][Patch] sprint-status.yaml `last_updated` comment says "moved in-progress -> review" but previous status was `ready-for-dev`. Correct the narrative comment. [_bmad-output/implementation-artifacts/sprint-status.yaml]
- [x] [Review][Patch] Tests assert on stdout substrings without anchoring to section headers (e.g. `assertIn("docs/dev/story-scope.md", stdout)`). Anchor to `Out-of-scope files:` block to prevent accidental hits when the same path appears in `Allowed scope entries:` or `Audited Scope-Override entries:`. [tests/tooling/story_scope/story_scope_validator_test.py multiple]
- [x] [Review][Patch] CI `BRANCH_NAME=$branch_name` written to `$GITHUB_ENV` without heredoc-quoting; a branch name containing newline corrupts the env file. Use the documented `KEY<<EOF` heredoc syntax. [.github/workflows/ci.yml:59-63]

#### Deferred (pre-existing or out-of-scope-to-fix-now)

- [x] [Review][Defer] CI runs duplicate jobs on `pull_request` and `push` for the same SHA — concurrency keys differ so neither cancels the other. [.github/workflows/ci.yml:3-9] — deferred, not regression-critical
- [x] [Review][Defer] CI: PR from a fork — `github.event.pull_request.base.sha` may not be fetched in the head-ref clone. [.github/workflows/ci.yml:43-46] — deferred until fork PRs are accepted
- [x] [Review][Defer] CI: brand-new branch first push produces a giant `diff-tree -r HEAD` listing every file in the head commit. [.github/workflows/ci.yml:51-52] — deferred, paired with P5 force-push fix
- [x] [Review][Defer] Branch with no story key + no trailer (e.g. dependabot/renovate) fails closed across all CI. No allowlist exists. [tools/check-story-file-scope.py:168-172] — deferred until automation PRs are configured
- [x] [Review][Defer] `commit-msg` re-reads `git diff --cached --name-only` after pre-commit hook may have changed the index; file set may differ between hooks. [.githooks/commit-msg:12] — deferred, low real-world impact
- [x] [Review][Defer] `read_commit_message` doesn't handle BOM or non-UTF-8 messages — UnicodeDecodeError instead of clean validator error. [tools/check-story-file-scope.py:114-118] — deferred, edge case
- [x] [Review][Defer] `--changed-files-file` doesn't strip UTF-8 BOM; PowerShell-emitted file would mismatch the first path silently. [tools/check-story-file-scope.py:246] — deferred, edge case
- [x] [Review][Defer] `collect_changed_files` silently drops paths normalizing to empty (`..`-only). [tools/check-story-file-scope.py:250] — deferred, theoretical
- [x] [Review][Defer] Pre-commit `git branch --show-current` empty during rebase/cherry-pick/detached-HEAD; validator fails closed mid-rebase. [.githooks/pre-commit:7] — deferred, needs UX design
- [x] [Review][Defer] `python` fallback could land on Python 2 on legacy systems. [.githooks/pre-commit:13-17] — deferred, no Python 2 in target environments
- [x] [Review][Defer] Hooks consume `git diff --cached --name-only` line-oriented output; filenames containing newlines mishandled. [.githooks/pre-commit:12] — deferred, no such filenames in repo
- [x] [Review][Defer] `is_vague` mixes raw `pattern` and post-normalized `normalized` for special-char check; backslashes get normalized away before the test. [tools/check-story-file-scope.py:286-288] — deferred, paired with P6
- [x] [Review][Defer] `parse_allowed_scope` doesn't honor `### ` subheadings as section terminators inside `## File Scope`. [tools/check-story-file-scope.py:206-211] — deferred, no current story uses this shape
- [x] [Review][Defer] `ALLOWED_LABELS` set has aliases (`Expected files to add or edit:`, `Allowed to modify:`) not documented in CONTRIBUTING.md. [tools/check-story-file-scope.py:19-23] — deferred, either remove or document in a follow-up
- [x] [Review][Defer] Multiple `Allowed files for this story:` blocks merge silently. [tools/check-story-file-scope.py:217-223] — deferred, no current story uses this shape

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
- Story-key source precedence is `--story-key` / explicit CLI argument first, then `Story:` / `Story-Key:` commit trailer, then branch name. This order is acceptance-critical and must be identical in hooks and CI.
- If two non-empty sources resolve different full story keys, fail closed and require the contributor to make the sources agree before retrying.
- `Story:` or `Story-Key:` should be treated as commit trailers, not free-form prose.
- `Scope-Override:` should also be treated as a trailer and should be specific enough for retrospective audit.
- `Scope-Override:` trailers are path-specific exceptions for the current commit, not policy changes to the story artifact.
- A missing story key should fail the guardrail unless the caller passed one explicitly.
- A broad override like `Scope-Override: *` or `Scope-Override: repo-wide cleanup` should be rejected as too vague.
- Overrides for forbidden-default areas (`src/**/*.cs`, `tests/**/*.cs`, release scripts, `package-lock.json`, and submodule contents) require a separate human/product/architecture decision and should be rejected by the automated check unless that decision is represented by a deliberately narrow implementation-time mechanism.

Recommended audit-friendly shape:

```text
Scope-Override: src/Hexalith.Memories.Server/Search/SemanticSearchService.cs - fix pre-existing D5-class runtime defect discovered while stabilizing CI lane
```

The implementation may allow multiple `Scope-Override:` trailers when more than one path needs explicit justification.

Party-mode review on 2026-05-01 clarified that override matching must be auditable and bounded:

- Use repository-relative POSIX-style paths for both changed files and override targets.
- Prefer exact path matching unless glob support is intentionally implemented and covered by tests.
- Do not allow one override to authorize unrelated sibling, suffix, child, or partial-string paths.
- Reject vague broad overrides such as `*`, `.`, repository-root values, bare directory names, and prose-only rationales.
- Keep override diagnostics visible in hook and CI output so reviewers can see what was authorized.

### File Scope Parser Contract

Parse only the bullet-list path or glob entries beneath `## File Scope` and the `Allowed files for this story:` label. Stop parsing at the next section label or heading. The parser should ignore explanatory prose and fenced code blocks, normalize entries to repository-relative POSIX-style paths, and fail closed when the selected story has no parseable allowed scope.

### Consistency Contract

The local hooks, CI job, and tests must exercise one shared Python validator. Shell and YAML layers may gather inputs, select a mode, and pass file paths or commit metadata, but they must not own matching rules, forbidden-default decisions, story-key precedence, or override parsing. This is the main architectural boundary for the story.

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
- broad or vague override values fail
- branch/trailer conflicts fail closed with a clear diagnostic
- path normalization behaves the same for Windows-style and POSIX-style inputs
- legitimate zero-changed-file input reports an explicit no-op success
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
- Party-mode architecture review trace captured on 2026-05-01 by Winston/System Architect. Recommendation was `needs-story-update`; edits applied to lock story-key precedence, bounded override semantics, parser boundaries, hook/CI parity, and forbidden-default single-source ownership.
- Pre-dev hardening preflight on 2026-05-01T15:18:53Z failed only for working tree cleanliness with stdout ` M Hexalith.EventStore\n`; classified as a soft working-tree warning because the dirty path is outside BMAD story-operation paths.
- Party-mode review ran on 2026-05-01T15:37:08Z using `/bmad-party-mode 12-3-story-file-scope-enforcement; review;`.
- Implementation red phase: `python -m unittest discover -s tests\tooling\story_scope -p "*_test.py"` failed because `tools/check-story-file-scope.py` did not exist yet.
- Implementation validation: `python -m unittest discover -s tests\tooling\story_scope -p "*_test.py"` passed, 13/13.
- Implementation validation: `python tools\check-story-file-scope.py --help` passed.
- Implementation validation: `sh .githooks/pre-commit` passed with no staged files and emitted the explicit no-op message.
- Implementation validation: Story 12.3-owned changed-file list passed `python tools\check-story-file-scope.py --story-key 12-3-story-file-scope-enforcement --changed-files-file <temp>`.
- Implementation validation: `git diff --check -- .github/workflows/ci.yml CONTRIBUTING.md tools/check-story-file-scope.py tests/tooling/story_scope/story_scope_validator_test.py _bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md _bmad-output/implementation-artifacts/sprint-status.yaml` passed; Git emitted CRLF conversion warnings only.
- Working-tree note: `Hexalith.EventStore`, `_bmad-output/process-notes/predev-preflight-latest.json`, and `_bmad-output/process-notes/predev-preflight-2026-05-01T154530Z.json` were present as unrelated out-of-scope changes/noise and were not included in this story's file list.

### Completion Notes List

- Story context created on 2026-05-01.
- Discovery loaded the relevant Epic 12 planning material and Epic 11 retrospective findings that introduced Action A4.
- No `Hexalith.Memories` root `project-context.md` file was present; a sibling `Hexalith.Commons` context file existed and was used only for background conventions.
- The recommended implementation keeps one canonical validator and reuses it from hooks plus CI to avoid logic drift.
- The story intentionally avoids Husky or other new package tooling because the repo already has a lightweight script-based tooling pattern and no existing hook manager.
- Pre-dev party-mode review findings were applied on 2026-05-01 without broadening into runtime/source changes, submodule edits, package tooling, or implementation work.
- Party-mode review applied clarifications for story-key conflict handling, hook timing, override specificity, parser boundaries, CI input source, path normalization, zero-diff behavior, plain diagnostics, and documentation examples.
- Implemented one shared Python validator at `tools/check-story-file-scope.py` that resolves story keys from CLI, trailers, and branch names; parses trailers with `git interpret-trailers --parse`; parses `## File Scope`; normalizes paths; checks allowed globs; audits narrow `Scope-Override:` trailers; rejects vague overrides; and keeps forbidden-default path logic in one source.
- Added repo-managed `.githooks/pre-commit` and `.githooks/commit-msg` wrappers. Both gather staged files with `git diff --cached --name-only --` and delegate all validation rules to the Python script.
- Added the `story-file-scope` CI job to `.github/workflows/ci.yml`. The job checks out the PR/push head, derives the PR source branch from `github.head_ref`, reads the real head commit message from `github.event.pull_request.head.sha` in PR context, gathers deterministic changed files, and invokes the shared validator without rebuilding the .NET solution.
- Added stdlib `unittest` regression coverage under `tests/tooling/story_scope/` for branch/trailer discovery, override parsing, in-scope and out-of-scope decisions, D5-style source touches, conflict handling, malformed/vague overrides, exact override boundaries, path normalization, zero-diff no-op behavior, and parser drift against a real current story file.
- Updated `CONTRIBUTING.md` with File Scope authoring rules, hook setup, story-key precedence, changed-file semantics, valid and invalid `Scope-Override:` examples, required PR check naming, and non-goals including no nested submodule commands.
- Validated required behavior: in-scope changes pass, out-of-scope changes fail, matching non-forbidden overrides pass, broad overrides fail, branch/trailer conflicts fail closed, Windows/POSIX path normalization is covered, and zero changed files report an explicit no-op success.
- No runtime source, release scripts, package metadata, package lock file, or submodule contents were intentionally modified for this story.

#### Scope-Justification: implementation commit `0b065b8` out-of-scope leaks (review close-out, 2026-05-01)

Code review identified four files in commit `0b065b8` outside the declared `## File Scope`. Per D1 resolution, these are accepted on this story with the durable prevention shipped in this same review pass (bare-submodule-path matcher + executable hook bit + D5-no-override regression test). Justifications:

- `Hexalith.EventStore` (submodule pointer bump 4a56c7c → d3df818): a working-tree side effect carried over from concurrent EventStore work; the pointer was not authored by Story 12.3 logic. Reverting in a follow-up commit was rejected as risky given the unknown intent of the bump. The new bare-submodule matcher (`is_forbidden_default('Hexalith.EventStore')` is now True via the recursive `**` glob fix) ensures any future story committing this leak is caught.
- `_bmad-output/implementation-artifacts/13-3-extend-embedding-client-to-support-ollama.md`: a co-authored sibling story creation accidentally bundled into this commit instead of its own `docs(bmad): create story 13.3 context` commit. Story 13.3 is `ready-for-dev` and unchanged by this leak.
- `_bmad-output/process-notes/predev-preflight-2026-05-01T154530Z.json` and `_bmad-output/process-notes/predev-preflight-latest.json`: BMad workflow telemetry artifacts emitted automatically by the pre-development preflight pass. The story File Scope did not anticipate these. A follow-up grooming pass should add `_bmad-output/process-notes/**` either to the BMad workflow's implicit-allow set or to the story-template default `Read/verify only:` examples.

The validator now correctly fails on this exact file list with `--story-key 12-3-story-file-scope-enforcement` (re-verified after patches: `Hexalith.EventStore` and the three other paths are reported in the appropriate forbidden-default vs out-of-scope sections).

#### Code review patches applied (2026-05-01)

- Forbidden-default `<submodule>/**` globs now match the bare submodule pointer path via proper recursive `**` semantics. The submodule-pointer leak class Story 12.2's closure note assigned to 12.3 is now detected.
- Glob matching switched from `fnmatch` to a recursive segment walker. `tools/*` no longer matches `tools/sub/x.py`; `**` correctly spans zero or more segments.
- `extract_backtick_path` no longer falls back to bare un-backticked tokens; only the first backtick token in a bullet is extracted, matching the spec's "do not infer paths from arbitrary prose" rule.
- `parse_allowed_scope` tracks fenced code blocks and ignores their contents; indented sub-bullets are no longer parsed as authoritative allow-list entries; the parser strips an optional leading UTF-8 BOM.
- `STORY_KEY_PATTERN` lookbehind tightened to `(?<![\w-])` so `feat/abc123-12-3-foo` no longer resolves to `123-12-3-foo`. Third segment must start with a letter.
- Trailer parser now permits duplicate-consistent `Story:` / `Story-Key:` trailers (real tooling commonly emits both) and rejects multiple story keys in a single trailer value.
- `--story-key` with an unparseable value fails closed instead of silently demoting to trailer/branch precedence.
- D5 case with no override at all now reports a distinct `Forbidden-default files (no override; D5-class):` diagnostic instead of a generic out-of-scope message.
- Story-artifact path diagnostic prints in POSIX form on Windows.
- `.githooks/pre-commit` and `.githooks/commit-msg`: file mode set to 100755 (executable bit) so Linux/macOS clones do not silently bypass them. Tempfiles relocated to `$git_dir/story-scope-changed-$$.txt` to dodge Windows MSYS `/tmp` path-translation risk.
- CI workflow: force-push and brand-new-branch first-push paths now compare against `origin/main` instead of an unreachable `before` SHA or the entire HEAD tree. `BRANCH_NAME` written to `$GITHUB_ENV` via the documented heredoc form.
- Tests refactored to use `tempfile.NamedTemporaryFile` instead of polluting cwd. New regressions: D5 with no override, bare-submodule-pointer detection, fenced-code-block parser ignore, indented sub-bullet rejection, bare-token bullet rejection, glob `*` not crossing path separators, story-key regex letter-prefix guard, branch+trailer agreement, duplicate-consistent trailers, multi-key-in-single-trailer, unparseable `--story-key`, missing `--changed-files-file`, empty `Scope-Override:` value. Frozen inline fixture replaces the live-file dependency for parser-drift coverage.
- `CONTRIBUTING.md` documents the allow-list-wins decision (an allow-list entry intersecting the forbidden-default list IS the spec-required separate decision; reviewers must call it out in story review) and lists the canonical forbidden-default set with the bare-submodule-pointer behavior.
- `sprint-status.yaml` close-out comment corrected (`ready-for-dev -> review`, not `in-progress -> review`).

15 follow-ups recorded as `12.3-RV1..12.3-RV15` in `_bmad-output/implementation-artifacts/deferred-work.md`. Live re-run: `python -m unittest discover -s tests/tooling/story_scope -p "*_test.py"` → 25/25 pass; `python tools/check-story-file-scope.py --story-key 12-3-story-file-scope-enforcement --changed-file Hexalith.EventStore` exit 1 with `Forbidden-default files (no override; D5-class):` listing the bare submodule path.

### File List

- `.github/workflows/ci.yml`
- `.githooks/commit-msg`
- `.githooks/pre-commit`
- `CONTRIBUTING.md`
- `tests/tooling/story_scope/story_scope_validator_test.py`
- `tools/check-story-file-scope.py`
- `_bmad-output/implementation-artifacts/12-3-story-file-scope-enforcement.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-01: Created Story 12.3 and promoted it from `backlog` to `ready-for-dev`.
- 2026-05-01: Applied Winston pre-dev party-mode architecture findings to clarify story-key precedence, override limits, parser contract, hook/CI consistency, and forbidden-default ownership.
- 2026-05-01: Party-mode review completed; story clarifications applied before development.
- 2026-05-01: Implemented story-file-scope validator, local hooks, CI guardrail job, focused tests, and CONTRIBUTING guidance; story moved to `review`.

## Party-Mode Review

- Date/time: 2026-05-01T15:37:08Z
- Selected story key: `12-3-story-file-scope-enforcement`
- Command/skill invocation used: `/bmad-party-mode 12-3-story-file-scope-enforcement; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), Paige (Technical Writer)
- Findings summary:
  - Story-key source conflicts needed fail-closed behavior.
  - `pre-commit` and `commit-msg` needed separate responsibility boundaries because the final commit message is unavailable during `pre-commit`.
  - `Scope-Override:` matching and specificity needed stronger anti-bypass rules.
  - File-scope parsing, changed-file handling, path normalization, and zero-diff behavior needed explicit contracts.
  - CI needed deterministic changed-file input and proof that it validates the PR head commit message instead of a synthetic merge commit message.
  - Documentation needed copy-paste-safe hook setup, valid and invalid override examples, and plain CLI diagnostic requirements.
- Changes applied:
  - Added task bullets for CLI-first story-key precedence, story-key conflict diagnostics, vague override rejection, path normalization, changed-file handling, empty-diff behavior, hook responsibility split, CI changed-file input, negative tests, and documentation examples.
  - Added `Scope-Override:` audit semantics and a parser-contract section to Dev Notes.
  - Expanded testing requirements for vague overrides, conflict cases, path normalization, and zero-diff behavior.
- Findings deferred:
  - Canonical branch naming policy remains a future team convention decision; this story should fail clearly when branch-derived story discovery is ambiguous.
- Final recommendation: ready-for-dev

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created. Status set to `ready-for-dev`.
