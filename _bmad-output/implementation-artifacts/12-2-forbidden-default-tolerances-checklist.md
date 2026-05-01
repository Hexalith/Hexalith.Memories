# Story 12.2: Forbidden Default Tolerances Checklist

Status: review

Story Key: 12-2-forbidden-default-tolerances-checklist
Epic: 12 - First Release & Operations Foundation
Created: 2026-05-01

## Story

As a code reviewer,
I want a written checklist of "tolerant defaults that hide failure" patterns to scan for,
so that the four tolerance-idiom silent-failure patterns surfaced in Epic 11 and equivalent shapes are caught at review time rather than after a green-but-broken pipeline reaches production.

## Acceptance Criteria

1. Given `CONTRIBUTING.md` has contributor workflow, CI, and release sections, when a contributor reads it before reviewing infrastructure, scripts, or workflow YAML changes, then it includes a Code Review section with a "Forbidden Default Tolerances" checklist.

2. Given the checklist is present, then it names at minimum these review-warning patterns:
   - Process-substitution or pipeline exit-code swallowing, such as `mapfile -t X < <(cmd)` losing `cmd`'s `exit 1`.
   - `actions/upload-artifact` `if-no-files-found: ignore` masking failed pack/build steps.
   - `dotnet nuget push --skip-duplicate` masking partial publish without an idempotency precondition and operator-visible signal.
   - Per-row or per-iteration zero-count silently passing aggregate verifiers.
   - Empty `catch { }` blocks in PowerShell or C# that swallow exceptions.
   - `|| true` and equivalent shell idioms that discard non-zero exit codes.
   - Default-empty-array fallbacks, such as `PROJECTS=("")`, that turn "no inventory" into "match everything."

3. Given some tolerant behavior is legitimate in narrow cases, then the checklist states the approval rule: a tolerant default is allowed only when it has an explicit idempotency proof, a clear recovery path, or an operator-visible alert; otherwise reviewers should request fail-fast behavior.

4. Given the checklist exists, then it cross-references Epic 11 retrospective Pattern 3 and the `feedback_tolerance_idioms.md` memory name so future agents loading memory know where the canonical guidance belongs. If the memory file is not present in the repository, do not invent it; reference the memory name and the retrospective source.

5. Given Story 12.2 is a documentation/governance story, then it does not change runtime source code, package inventory, release behavior, CI behavior, or tests.

6. Given a future PR introduces one of the listed patterns, then a reviewer can point at the new checklist as the basis for requesting a change instead of relitigating the rationale.

## Tasks / Subtasks

- [x] Task 1 - Add the Code Review section to `CONTRIBUTING.md` (AC: 1, 2, 3, 6)
  - [x] Add a `## Code Review` section after `## Pull Request CI` and before `## Release Packages`, unless the contributor guide has been reorganized by the time this story is implemented.
  - [x] Add a `### Forbidden Default Tolerances` subsection with the seven required patterns from AC #2.
  - [x] Phrase the checklist as reviewer guidance, not as implementation of a new automated gate.
  - [x] Include the approval rule from AC #3: idempotency proof, recovery path, or operator-visible alert.

- [x] Task 2 - Preserve existing contributor guidance (AC: 1, 5)
  - [x] Keep the existing Setup, Branches and Pull Requests, Conventional Commits, Tests, Pull Request CI, Release Packages, and Release Process guidance intact.
  - [x] Do not change command examples except for local copy-editing required by the new section.
  - [x] Do not alter the documented stable CI checks: `build`, `test-unit-contract`, and `integration-fast`.
  - [x] Do not alter release package inventory or publish instructions.

- [x] Task 3 - Add source references for future reviewers and agents (AC: 4, 6)
  - [x] Reference `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md` Pattern 3.
  - [x] Reference `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-30.md` "Tolerant defaults repeatedly hid failure."
  - [x] Reference the memory name `feedback_tolerance_idioms.md`. Treat it as an external/auto-memory artifact because no file with that name is currently present under the repository.
  - [x] Optionally mention Story 12.5 owns executable partial-publish alerting; this story only creates reviewer guidance.

- [x] Task 4 - Validate the documentation-only change (AC: 1-6)
  - [x] Review `CONTRIBUTING.md` manually for heading order, Markdown formatting, and actionable wording.
  - [x] Run a focused grep check that the seven required phrases or their clear equivalents are present.
  - [x] Confirm the diff touches only `CONTRIBUTING.md` and this story file, unless the BMad workflow updates `sprint-status.yaml`.
  - [x] Do not run a full build unless implementation unexpectedly changes code or scripts.

## File Scope

Allowed files for this story:

- `CONTRIBUTING.md` - UPDATE. Main deliverable: add Code Review / Forbidden Default Tolerances guidance.
- `_bmad-output/implementation-artifacts/12-2-forbidden-default-tolerances-checklist.md` - UPDATE Dev Agent Record and completion notes.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad workflow state transitions.

Read/verify only:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-26.md`
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md`
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-30.md`
- `_bmad-output/implementation-artifacts/12-1-first-release-path-validation.md`
- `.github/workflows/ci.yml`
- `.github/workflows/release.yml`
- `tools/test.sh`
- `tools/test.ps1`
- `tools/test-release.ps1`
- `tools/verify-integration-fast-coverage.py`
- `tools/publish-nuget.ps1`

Forbidden by default:

- `src/**/*.cs`
- `tests/**/*.cs`
- `.github/workflows/*.yml`
- `tools/*.ps1`
- `tools/*.sh`
- `tools/*.py`
- `tools/release-packages.json`
- Package metadata, public API contracts, runtime behavior, or CI/release behavior changes

If implementation discovers an actual broken tolerance in scripts or workflow YAML, do not fix it inside this story unless Jerome explicitly redirects scope. Capture it as a follow-up or hand it to Story 12.3, 12.4, or 12.5 as appropriate.

## Dev Notes

### Epic Context

Epic 12 is an operations and first-release follow-through epic. It exists to prove release readiness, apply process guardrails, and operationalize Epic 11 retrospective lessons before further feature investment. Story 12.2 specifically operationalizes Epic 11 action A3: make silent-failure tolerance patterns reviewable from `CONTRIBUTING.md`.

Do not broaden this story into Phase 2 feature work, runtime hardening, file-scope automation, baseline cleanup, or partial-publish alerting. Those are separate Epic 12 stories:

- Story 12.3 owns file-scope enforcement and `Scope-Override:` mechanics.
- Story 12.4 owns baseline-failure sweep and executable inventory checks.
- Story 12.5 owns partial-publish alerting in `tools/publish-nuget.ps1` / release operations.
- Story 12.6 owns the S11-FA baseline test resolution.

### Current State of `CONTRIBUTING.md`

`CONTRIBUTING.md` currently contains:

- Setup and submodule initialization guidance.
- Branch and PR naming guidance.
- Conventional commit rules.
- Test commands for Docker-free and Docker-backed lanes.
- Pull Request CI details, including stable checks `build`, `test-unit-contract`, and `integration-fast`.
- Release package inventory.
- Release process, `NUGET_API_KEY`, and local release validation commands.

It does not currently contain a `## Code Review` section or a `Forbidden Default Tolerances` checklist. The new section should fit between Pull Request CI and Release Packages because it applies to reviewers before package/release-specific material.

### Current State of Tolerance Guidance

The canonical source is the Epic 11 retrospective, not an existing contributor-guide section:

- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md` Pattern 3 lists four discovered silent-failure idioms: process-substitution exit-code swallowing, `if-no-files-found: ignore`, `--skip-duplicate` masking partial publish, and per-project zero-test silent pass.
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-30.md` restates the lesson as "Tolerant defaults repeatedly hid failure" and calls these governance issues rather than isolated syntax mistakes.
- `sprint-change-proposal-2026-04-26.md` says the auto-memory was already updated with `feedback_tolerance_idioms.md`, but no file named `feedback_tolerance_idioms.md` is present under this repository as of story creation. Reference the memory name, but do not create a memory file unless a separate memory workflow is invoked.

### Required Checklist Semantics

The checklist should teach reviewers to ask two questions:

1. Does this code intentionally tolerate a missing artifact, duplicate package, zero row count, swallowed error, missing inventory, or failed command?
2. If yes, is there an explicit proof that the tolerance is safe, such as idempotency, a recovery path, a structured warning, or an alert that an operator will see?

If the answer to #2 is no, reviewer guidance should request fail-fast behavior or a visible signal.

Keep examples concrete and tied to this repository:

- `actions/upload-artifact` with `if-no-files-found: ignore` can hide failed pack/build output. Prefer `error`, or use `warn` only when absence is explicitly non-blocking and documented.
- `dotnet nuget push --skip-duplicate` is acceptable only for 409-conflict rerun self-healing. It is not a general substitute for partial-publish detection; Story 12.5 owns adding an operator-visible partial-publish signal.
- Process substitution runs the substituted command asynchronously in Bash. Do not rely on `mapfile -t X < <(cmd)` alone as evidence that `cmd` succeeded; capture command output/status explicitly.
- Zero-count guards must aggregate all rows/projects and fail if the selected test or verification surface executed nothing.
- Empty `catch { }`, `|| true`, `SilentlyContinue`, and default-empty inventory fallbacks need a written reason and an alternate signal.

### Architecture and Project Rules

This is documentation-only work, but still follow project hygiene:

- Preserve existing Markdown style in `CONTRIBUTING.md`: short sections, tables where useful, fenced command blocks for examples.
- Keep guidance actionable and scannable. Avoid long narrative.
- Do not add dependencies or package versions.
- Do not initialize or update nested submodules.
- Do not touch `Hexalith.Builds` or other submodule contents.
- Do not modify runtime source or test files for this story.

### Previous Story Intelligence

Story 12.1 completed and was code-reviewed on 2026-04-30. Carry forward these lessons:

- Epic 12 stories need strict file scope. Story 12.1 explicitly kept tolerance checklist work in Story 12.2.
- Documentation precision matters. Story 12.1 review applied 15 documentation-precision patches, including clarifying release sequencing, branch-protection evidence, and `--skip-duplicate` partial-publish behavior.
- Keep "what is observed" separate from "what is planned." For this story, observed evidence is Epic 11's tolerance failures; planned executable enforcement belongs to later stories.
- Do not claim a file or memory exists if it does not. The `feedback_tolerance_idioms.md` memory is referenced by planning artifacts, but it is not currently a repository file.

Recent git history before story creation also shows active release/path hardening work:

- `1b09ee4 fix: update subproject commit reference in Hexalith.AI.Tools`
- `dde76c3 Merge pull request #13 from Hexalith/fix/release-protected-main-semantic-release`
- `ad5aa78 fix(apphost): write Dapr Redis components from allocated endpoint`
- `32112ad test: wait for Redis protocol readiness before Dapr`
- `103e62e fix(apphost): wait for Redis before server sidecar`

The recent commits include runtime fixes outside this story's scope. Do not use Story 12.2 as a vehicle for similar runtime stabilization.

### Latest Technical Information

Web verification was performed on 2026-05-01:

- `actions/upload-artifact` supports `if-no-files-found` values `warn`, `error`, and `ignore`; the current default is `warn`, and `error` is available for fail-fast behavior. Source: https://github.com/actions/upload-artifact/blob/main/README.md
- `dotnet nuget push --skip-duplicate` treats HTTP 409 Conflict responses as warnings so other pushes can continue. This supports duplicate-rerun recovery, not general partial-publish invisibility. Source: https://learn.microsoft.com/dotnet/core/tools/dotnet-nuget-push
- Bash process substitution runs the process list asynchronously and passes a filename to the current command. This is why reviewers should be wary of assuming the producer command's exit status was enforced by the consumer command. Source: https://www.gnu.org/software/bash/manual/html_node/Process-Substitution.html
- PowerShell `catch` blocks are the place to track or recover from terminating errors; `$ErrorActionPreference` values such as `SilentlyContinue` or `Ignore` can suppress propagation. Empty catches therefore need explicit justification and an alternate signal. Source: https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_try_catch_finally

### Testing Requirements

Minimum validation for this documentation-only story:

```powershell
rg -n "Code Review|Forbidden Default Tolerances|if-no-files-found|skip-duplicate|mapfile|zero-count|catch \\{ \\}|\\|\\| true|PROJECTS=\\(\\\"\\\"\\)|feedback_tolerance_idioms" CONTRIBUTING.md
git diff -- CONTRIBUTING.md _bmad-output/implementation-artifacts/12-2-forbidden-default-tolerances-checklist.md
```

Manual review must confirm:

- The new section is readable before Release Packages.
- All AC #2 patterns or clear equivalents are present.
- The guidance does not tell reviewers to reject every tolerance unconditionally; it allows tolerance with proof, recovery, or alerting.
- The diff does not modify code, tests, workflows, package inventory, or release scripts.

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 12 and Story 12.2 acceptance criteria.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-04-26.md` - Option C and A3 tolerance-checklist scaffold.
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-26.md` - Pattern 3 and A3 action item.
- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-30.md` - refreshed Epic 11 carry-forward findings.
- `_bmad-output/implementation-artifacts/12-1-first-release-path-validation.md` - previous Epic 12 story and scope lessons.
- `CONTRIBUTING.md` - target contributor guide.
- `feedback_tolerance_idioms.md` - referenced memory name from planning artifacts; not present as a repository file at story creation.

## Dev Agent Record

### Agent Model Used

GPT-5.

### Debug Log References

- `rg -n "Code Review|Forbidden Default Tolerances|if-no-files-found|skip-duplicate|mapfile|zero-count|catch \\{ \\}|\\|\\| true|PROJECTS=\\(\\\"\\\"\\)|feedback_tolerance_idioms" CONTRIBUTING.md` initially failed because shell quoting stripped part of the regex around `PROJECTS=("")`; reran validation with fixed-string checks.
- Focused checklist validation: all required phrases were found in `CONTRIBUTING.md`.
- Diff review: only `CONTRIBUTING.md`, `_bmad-output/implementation-artifacts/12-2-forbidden-default-tolerances-checklist.md`, and `_bmad-output/implementation-artifacts/sprint-status.yaml` are touched by this story pass.
- Full build/test suite intentionally not run because this is a documentation/governance-only story and no runtime source, tests, workflows, release scripts, or package inventory changed.

### Completion Notes List

- Story context created on 2026-05-01.
- Discovery loaded whole planning artifacts: `prd.md`, `architecture.md`, and `epics.md`; no UX artifact matched the workflow pattern.
- Sprint status selected this as the first backlog story in file order: `12-2-forbidden-default-tolerances-checklist`.
- `feedback_tolerance_idioms.md` was referenced by planning artifacts but not found as a repository file.
- Added `## Code Review` after `## Pull Request CI` and before `## Release Packages` in `CONTRIBUTING.md`.
- Added `### Forbidden Default Tolerances` checklist covering all seven required tolerance-warning patterns.
- Added the approval rule: tolerant defaults are allowed only with explicit idempotency proof, clear recovery path, or operator-visible warning/failure/alert; otherwise reviewers should request fail-fast behavior.
- Added Epic 11 retrospective references and the external/auto-memory name `feedback_tolerance_idioms.md`.
- Preserved existing setup, branch, conventional commit, test, PR CI, release package, and release process guidance without changing CI checks, package inventory, release scripts, source, or tests.

### File List

- `CONTRIBUTING.md`
- `_bmad-output/implementation-artifacts/12-2-forbidden-default-tolerances-checklist.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-01: Implemented documentation-only forbidden default tolerances checklist and moved story to review.

## Story Completion Status

Implementation complete. Status set to `review`.
