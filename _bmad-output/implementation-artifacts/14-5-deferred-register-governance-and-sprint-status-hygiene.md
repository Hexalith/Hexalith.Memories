# Story 14.5: Deferred Register Governance and Sprint-Status Hygiene

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want deferred-work entries and sprint-status history to stay auditable,
so that future planning can distinguish open risk, resolved risk, accepted risk, and stale historical noise without manual archaeology.

## Acceptance Criteria

1. Given `_bmad-output/implementation-artifacts/deferred-work.md` remains the canonical deferred register, when new or migrated entries are written, then each entry has a minimal consistent structure for ID, status, source story, target artifact, and re-open trigger.

2. Given Epic 14 stories resolve deferred items, when each story completes, then its targeted deferred entries are updated as `resolved`, `accepted`, or `carried-forward` with validation evidence or rationale.

3. Given `_bmad-output/implementation-artifacts/sprint-status.yaml` records history, when future status updates are appended, then guidance avoids unbounded one-line history comments and prefers concise dated notes.

4. Given tests or scripts parse deferred-work entries, when the register structure changes, then those tests or scripts are updated to parse the new structure without broad author-controlled substring heuristics.

5. Given this governance story touches planning and tracking files, when it is implemented, then it avoids submodule pointer changes and follows root-level submodule discipline.

## Tasks / Subtasks

- [x] Task 1 - Define the deferred-work entry schema and migration rule (AC: 1, 2, 4)
  - [x] Add a short schema guide near the top of `_bmad-output/implementation-artifacts/deferred-work.md` that defines required fields for active entries: `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, and either `Evidence` or `Rationale`.
  - [x] Use the allowed status vocabulary `open`, `resolved`, `accepted`, and `carried-forward`. Do not introduce near-synonyms such as `done`, `closed`, `fixed`, or `deferred-again`.
  - [x] Preserve historical prose where it carries useful context, but add structured field lines so tools do not need to infer status or classification from arbitrary paragraph text.
  - [x] State that completed or accepted entries must retain enough evidence to explain why the risk is no longer open.
  - [x] Keep the schema Markdown-readable. Do not replace the register with JSON/YAML unless explicitly approved.

- [x] Task 2 - Migrate the Story 14.5 target deferred IDs (AC: 1, 2, 4)
  - [x] Update `12.4-RV6` so the baseline/filter parser risk has structured fields and points to the new parser contract in `CiTestInventoryTests`.
  - [x] Update `12.4-RV19` so `DeferredKeyRegex` format brittleness is either resolved by parser changes or carried forward with the exact accepted key grammar and trigger.
  - [x] Update `13.7-RV5` so sprint-status long-line history is either resolved by the new guidance/tooling or carried forward with a concrete trigger.
  - [x] Revisit adjacent `12.6-RV2` because it explicitly says it realizes `12.4-RV6`; close, accept, or carry it forward consistently with the new schema.
  - [x] Do not bulk-migrate all 266 historical entries. Convert only the Story 14.5 target set plus a tiny representative fixture section if tests require it.

- [x] Task 3 - Replace deferred-work prose heuristics in CI inventory tests (AC: 4)
  - [x] Update `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` so deferred baseline parsing reads structured fields instead of classifying entries by substring checks such as `baseline`, `test-release.ps1`, or `release lane`.
  - [x] Replace `DeferredKeyRegex` with a parser that accepts the documented structured ID field and does not depend on a literal period after `S11-F*`.
  - [x] Keep closed/resolved entries out of the active baseline set by reading the structured status field instead of looking for `[resolved`, `[closed`, or `[done]` in the heading text.
  - [x] Add fixture tests for `open`, `resolved`, `accepted`, and `carried-forward` entries, including a migrated S11-F baseline entry and a non-baseline deferred item.
  - [x] Keep the zero-accepted-release-filter behavior intact when no active baseline entries claim a release filter.

- [x] Task 4 - Add sprint-status history guidance without rewriting history (AC: 3, 5)
  - [x] Add concise guidance to `CONTRIBUTING.md` or another existing contributor/process document that says sprint-status updates should use short dated notes and avoid appending multi-sentence narratives to a single YAML line.
  - [x] Recommend putting detailed evidence in the story artifact, `deferred-work.md`, or a dedicated review/retro document, then linking or naming that artifact from `sprint-status.yaml`.
  - [x] Do not rewrite completed Epic 1-13 story history comments as part of this story. Historical cleanup is out of scope unless a parser failure proves a targeted edit is required.
  - [x] If a helper script is added, keep it advisory or validation-focused. It must not auto-reformat sprint status without an explicit maintainer command.

- [x] Task 5 - Update Epic 14 bookkeeping rules for future story close-out (AC: 2, 3, 5)
  - [x] Add or update guidance that Epic 14 implementation stories must list every targeted deferred ID in completion notes and mark it `resolved`, `accepted`, or `carried-forward`.
  - [x] Make clear that discussing an item is not closure. Closure requires code, test, documentation, or explicit acceptance evidence.
  - [x] Record any entries intentionally left open with a fresh `Re-open trigger` and rationale rather than silently removing them.
  - [x] Keep root-level submodule pointer changes forbidden by default and never initialize/update nested submodules for this story.

- [x] Task 6 - Validate the governance lane (AC: 1-5)
  - [x] Run `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~CiTestInventoryTests"`.
  - [x] Run `git diff --check -- _bmad-output/implementation-artifacts/deferred-work.md tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs CONTRIBUTING.md _bmad-output/implementation-artifacts/14-5-deferred-register-governance-and-sprint-status-hygiene.md`.
  - [x] If a new tooling test harness is added, run its focused command and record the exact command/output summary in the Dev Agent Record.
  - [x] Record before/after counts for the targeted deferred IDs only. Do not report broad deferred-register cleanup unless it was actually performed and scoped.

### Review Findings

- [x] [Review][Patch] Structured entries without `ID:` are silently ignored [tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:821] -- AC1 makes `ID` a required field and the story clarifications require malformed or missing required fields to fail closed, but `ParseStructuredDeferredEntries` skips every structured field seen before the first `ID:`. A future migrated active entry that starts with `Status: open` / `Target artifact: tools/test-release.ps1` but omits or misspells `ID:` will disappear from the parser instead of failing validation, so the register can lose an active risk while CI remains green. Fixed in review close-out by failing recognized structured fields that appear before `ID:` and adding an `ID` missing-field fixture.
- [x] [Review][Patch] Status-specific evidence rules are documented but not enforced [tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:866] -- The schema says `Evidence:` is required for `resolved`, while `Rationale:` is required for `accepted` and `carried-forward`, but `FlushPendingStructuredEntry` only checks that either field is present. That lets `resolved` entries pass with only rationale, and `accepted` / `carried-forward` entries pass with only evidence, weakening the audit distinction this story is meant to create. Fixed in review close-out by enforcing status-specific `Evidence:` / `Rationale:` requirements and adding negative fixtures.
- [x] [Review][Patch] Scope validation evidence is contradicted by an untracked solution file [_bmad-output/implementation-artifacts/14-5-deferred-register-governance-and-sprint-status-hygiene.md:248] -- The Dev Agent Record says `git status` only shows the allowed story files and that no package/project metadata changed, but current `git status --short` also reports `?? Hexalith.Memories.sln`. The story forbids package/project metadata changes by default, so either the untracked solution file must be removed from the working tree or the validation/story status cannot claim the scope is clean. Fixed in review close-out by removing the untracked `Hexalith.Memories.sln` from the working tree.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/deferred-work.md` - UPDATE. Add schema guidance and migrate only the Story 14.5 target entries plus any small fixtures required for parser coverage.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` - UPDATE. Parse structured deferred-work fields instead of author-controlled prose heuristics.
- `CONTRIBUTING.md` - UPDATE. Add concise guidance for deferred-work entry structure and sprint-status history comments.
- `_bmad-output/implementation-artifacts/14-5-deferred-register-governance-and-sprint-status-hygiene.md` - UPDATE. Record implementation notes, validation, review findings, and file list.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad workflow/status transitions and concise status-history notes.

Optional, only if needed:

- `tools/check-deferred-work-format.py` - NEW optional. Use only if a small stdlib checker gives clearer validation than embedding all parsing in C# tests.
- `tests/tooling/deferred_work/` - NEW optional. Use only if the optional checker is added.
- `docs/dev/release-runbook.md` - UPDATE only if a release-lane baseline bookkeeping note must move out of deferred-work prose.

Read/verify only:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-03.md`
- `_bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md`
- `_bmad-output/implementation-artifacts/12-6-embedding-input-content-kind-baseline-resolution.md`
- `_bmad-output/implementation-artifacts/13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide.md`
- `_bmad-output/implementation-artifacts/14-1-ci-story-scope-enforcement-hardening.md`
- `_bmad-output/implementation-artifacts/14-2-release-pipeline-audit-hardening.md`
- `_bmad-output/implementation-artifacts/14-3-oidc-and-embedding-security-hardening.md`
- `_bmad-output/implementation-artifacts/14-4-migration-and-integration-test-hardening.md`
- `tools/test-release.ps1`

Forbidden by default:

- `.github/**`
- `src/**`
- `tests/**/*.cs` except `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `tools/test-release.ps1` except read-only verification
- `tools/validate-release-packages.ps1`
- `tools/publish-nuget.ps1`
- `tools/pack-release.ps1`
- `Directory.Packages.props`
- `Directory.Build.props`
- `package-lock.json`
- `Hexalith.AI.Tools/**`
- `Hexalith.Commons/**`
- `Hexalith.EventStore/**`

## Dev Notes

### Current Implementation State

`_bmad-output/implementation-artifacts/deferred-work.md` is a human-readable Markdown register with grouped bullet entries. It contains structured-looking IDs, but active status, source story, target artifact, re-open trigger, baseline classification, and release-filter linkage are mostly embedded in prose. That shape has already caused follow-up findings:

- `12.4-RV6` says `baselineRelated` and `HasReleaseFilter` in `CiTestInventoryTests` rely on author-controlled prose tokens.
- `12.4-RV19` says `DeferredKeyRegex` only recognizes uppercase `S11-F[A-Z0-9]+.` headings with a literal period.
- `12.6-RV2` repeats the same concern and names the current substring classifier as the reason a prose edit could break tests.
- `13.7-RV5` records the sprint-status long-line pattern as a project-wide hygiene issue that needs coordinated convention change.

`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` currently reads `tools/test-release.ps1` filters and open deferred baseline entries. The important helper methods are `ReadOpenDeferredBaselines(...)`, `SplitDeferredEntries(...)`, `ParseDeferredBaseline(...)`, `DeferredKeyRegex()`, `ProjectFilterRegex()`, and `DeferredTestNameRegex()`. The current parser intentionally skips sections after `## Closed by:` and treats `[resolved`, `[closed`, or `[done]` markers in the first line as closed. Story 14.5 should preserve the business rule but move the source of truth to structured fields.

`tools/test-release.ps1` currently has no accepted release-lane baseline filters. Do not add a filter for this story. The parser change must keep the invariant that filters are empty when no active structured deferred baseline entries claim a release filter.

`sprint-status.yaml` currently stores large historical comments on individual story status lines. Do not rewrite those historical comments in bulk. The safer change is a forward-looking convention: concise dated status notes in sprint status, detailed evidence in story artifacts, review traces, run logs, or `deferred-work.md`.

### Deferred IDs Targeted

This story is the normal lifecycle home for:

- `12.4-RV6`: Deferred-work parser heuristics based on substrings in prose.
- `12.4-RV19`: S11 deferred-key regex format brittleness.
- `12.6-RV2`: Follow-on baseline classifier heuristic risk that realizes `12.4-RV6`.
- `13.7-RV5`: Sprint-status history comments growing into multi-thousand-character YAML lines.

Do not sweep adjacent Epic 14 implementation entries into this story. Stories 14.1 through 14.4 own their targeted `12.x`, `13.x`, `S11-*`, and migration/security/release entries. Story 14.5 owns the register schema and hygiene conventions that those stories will use.

### Implementation Guardrails

- Keep this a governance and parser hardening story. Do not change runtime code, CI workflows, release scripts, provider code, migration code, package metadata, or submodules.
- Prefer a minimal Markdown field schema over a wholesale register rewrite. The register still needs to be readable in code review.
- Do not delete historical evidence while migrating entries. If an entry is marked resolved or accepted, retain the evidence or rationale that supports that state.
- Avoid broad substring heuristics in tests. Once a structured field exists, parse the field value directly.
- Do not create a second canonical deferred-work register. If optional tooling is added, it validates `_bmad-output/implementation-artifacts/deferred-work.md`; it does not own a new source of truth.
- Keep parser changes deterministic and local. `CiTestInventoryTests` should not require network, GitHub, release artifacts, Docker, DAPR, or Aspire.
- Do not initialize or update nested submodules. Do not change root-level submodule pointers.
- Before moving the story to review, verify the diff contains no runtime source, CI workflow, package/project metadata, release script, or submodule pointer changes.

### Party-Mode Review Clarifications - 2026-05-04

- Define a canonical Markdown field block for each Story 14.5 target entry before changing parser behavior. The exact field labels are `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, and either `Evidence` or `Rationale`; tests must parse these labels as structure rather than matching surrounding prose.
- Keep the status vocabulary closed and lowercase: `open` means planned action is still needed; `resolved` means evidence shows the risk no longer applies; `accepted` means the risk remains but is intentionally accepted with rationale; `carried-forward` means the risk remains and is moved to a named future artifact or trigger.
- Migrate only `12.4-RV6`, `12.4-RV19`, `12.6-RV2`, and `13.7-RV5` to the structured field block. Do not normalize unrelated historical entries, bulk-migrate the deferred register, or require legacy prose-only entries to satisfy the new schema unless they are deliberately touched by a future story.
- Preserve historical context around the migrated entries, but make the structured fields the source of truth for tests and planning. A maintainer must be able to determine each targeted risk's current disposition without reading arbitrary narrative paragraphs.
- Parser updates must be field-aware and ID-exact. Add negative coverage proving unrelated prose mentions do not count as entries, malformed or missing required fields fail closed, invalid statuses such as `done` or `closed` are rejected, and IDs like `12x4-RV6` or `112.4-RV6` do not match `12.4-RV6`.
- Keep `CiTestInventoryTests` tolerant of historical noise while strict for structured target entries. Test fixtures should include `open`, `resolved`, `accepted`, and `carried-forward` examples, a migrated `S11-F*` release-baseline entry, and a non-baseline entry whose narrative still contains words like `baseline`, `release lane`, or `test-release.ps1`.
- Sprint-status hygiene is forward-looking. Add guidance that future `sprint-status.yaml` notes should be short dated breadcrumbs to story artifacts, deferred-work IDs, run logs, or review documents instead of accumulating multi-sentence evidence on one YAML line; do not rewrite Epic 1-13 history comments as part of this story.
- If an optional checker is added, keep it small, stdlib-only, and scoped to validating `_bmad-output/implementation-artifacts/deferred-work.md` field blocks and status vocabulary. Do not turn it into a general Markdown linter or new canonical registry.
- Do not change runtime behavior, CI configuration, release scripts, package metadata, production source, or submodule pointers. Do not initialize or update nested submodules.

### Technical Constraints and References

- The repository uses C# xUnit and Shouldly for `CiTestInventoryTests`; keep assertions in that style and avoid adding a Markdown parser package just for this story.
- The project context prefers `ValueOrError<T>` and strict C# analyzer hygiene for production code, but this story should not touch production C# code.
- Markdown field lines can be parsed with anchored regular expressions because the target schema is intentionally small. Avoid relying on arbitrary prose after the field block.
- YAML comments in `sprint-status.yaml` are not a durable evidence store. Treat them as navigation breadcrumbs to dated story artifacts or process notes.

No current external technology research was needed for this story. The implementation surface is repository-owned Markdown conventions and existing test code.

### Testing Requirements

Minimum validation before review:

```powershell
dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~CiTestInventoryTests"
git diff --check -- _bmad-output/implementation-artifacts/deferred-work.md tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs CONTRIBUTING.md _bmad-output/implementation-artifacts/14-5-deferred-register-governance-and-sprint-status-hygiene.md
```

Additional probes to record when relevant:

- A structured open `S11-F*` baseline entry with a release filter field is matched to `tools/test-release.ps1`.
- A structured `resolved` or `accepted` S11 entry is excluded from active baseline filters.
- A non-baseline deferred item containing the words `baseline`, `release lane`, or `test-release.ps1` in narrative prose is not misclassified unless the structured fields say it is a baseline filter item.
- `12.4-RV6`, `12.4-RV19`, `12.6-RV2`, and `13.7-RV5` are each marked `resolved`, `accepted`, or `carried-forward` with evidence or rationale.
- Sprint-status guidance points developers to story artifacts or deferred-work entries for detail instead of encouraging long inline YAML comments.
- Required-field parser fixtures cover every missing-field case, one invalid status, and exact-ID boundary cases for dotted IDs.
- Repository-scope validation records that no forbidden paths, package/project files, CI or release scripts, runtime source files, or submodule pointers changed.

## Project Structure Notes

- This is a governance and test-parser story. Expected implementation stays under BMAD artifacts, contributor guidance, and one focused C# CI inventory test file.
- Use existing repository conventions: Markdown for process artifacts, xUnit + Shouldly for C# tests, PowerShell examples for validation commands, and no new package dependencies unless justified.
- The `Hexalith.Commons` `project-context.md` discovered by the persistent-facts glob is background Hexalith guidance only because it belongs to a submodule/sibling repository. Story-local file scope and repository-specific artifacts are authoritative.

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 14 and Story 14.5 acceptance criteria.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-03.md` - approved Epic 14 scope and target IDs for Story 14.5.
- `_bmad-output/implementation-artifacts/deferred-work.md` - canonical deferred register and target entries `12.4-RV6`, `12.4-RV19`, `12.6-RV2`, and `13.7-RV5`.
- `_bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md` - accepted-baseline guardrails and source of `12.4-RV6`/`12.4-RV19`.
- `_bmad-output/implementation-artifacts/12-6-embedding-input-content-kind-baseline-resolution.md` - source of `12.6-RV2` follow-on classifier concern.
- `_bmad-output/implementation-artifacts/13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide.md` - source of `13.7-RV5` sprint-status long-line concern.
- `_bmad-output/implementation-artifacts/14-4-migration-and-integration-test-hardening.md` - previous Story 14 context and explicit handoff that sprint-status cleanup remains for Story 14.5.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` - current deferred baseline parser and release-lane inventory guard tests.
- `tools/test-release.ps1` - release-lane filter source that must remain in sync with structured deferred baseline entries.
- `CONTRIBUTING.md` - likely home for forward-looking contributor/process guidance.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Pre-dev hardening preflight JSON timestamp `2026-05-03T11:55:08Z` passed all checks with `working tree cleanliness` reporting `0 dirty paths`.
- Story selection chose `14-5-deferred-register-governance-and-sprint-status-hygiene` because `ready_count` was `4`, below the target of `5`, and this was the first backlog story in sprint-status order.
- `/bmad-create-story 14-5-deferred-register-governance-and-sprint-status-hygiene` context gathering loaded Epic 14 planning, the approved 2026-05-03 sprint-change proposal, Stories 12.4, 12.6, 13.7, and 14.1-14.4 context, current deferred-work entries, current `CiTestInventoryTests`, recent git history, and project context facts.

### Completion Notes List

- Story context created on 2026-05-03.
- Scope is limited to deferred-register structure, targeted deferred-entry migration, CI inventory parser updates, sprint-status guidance, and bookkeeping validation.
- Runtime source, CI workflows, release scripts, migration code, provider code, package metadata, and submodules are forbidden by default.
- No submodule state was touched.

#### Dev Implementation 2026-05-04

- Added a "Schema for Active Entries" section near the top of `_bmad-output/implementation-artifacts/deferred-work.md` documenting the canonical Markdown field block (`ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, `Evidence`/`Rationale`, optional `Test`) and the closed lowercase status vocabulary (`open`, `resolved`, `accepted`, `carried-forward`).
- Added the "Closed by: Story 14.5 Deferred Register Governance and Sprint-Status Hygiene (2026-05-04)" rollup section with structured field blocks for the four targeted IDs:
  - `12.4-RV6` — `Status: resolved` — closed by the structured-field parser.
  - `12.4-RV19` — `Status: resolved` — `DeferredKeyRegex`'s literal-period dependency removed; IDs now read from the structured `ID:` field verbatim.
  - `12.6-RV2` — `Status: resolved` — closed alongside `12.4-RV6`; new fixture proves prose mentions of `baseline`/`release lane`/`tools/test-release.ps1` no longer drive classification.
  - `13.7-RV5` — `Status: resolved` — sprint-status hygiene captured as forward-looking guidance in `CONTRIBUTING.md`.
- Tagged each original entry in its source-section with `[resolved in 14.5]` so prose readers see the disposition without consulting the rollup.
- Replaced the substring-driven baseline classifier in `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`. The new `ParseStructuredDeferredEntries` reader uses the anchored `StructuredFieldRegex`, validates the closed status vocabulary via `AllowedDeferredStatuses`, requires every documented field, enforces ID-exact equality through `StructuredIdShape`, rejects duplicated fields, and demands the `Test:` field when `Target artifact` equals `tools/test-release.ps1`. `DeferredKeyRegex` is retained for parsing PowerShell comments in `tools/test-release.ps1` only; the deferred-work parser no longer uses it.
- Added 18 fixture tests covering: structured open S11-F baseline returns; resolved/accepted/carried-forward statuses skipped; prose mentions not misclassified; legacy entries without structured blocks ignored; missing `Status`/`Source story`/`Target artifact`/`Re-open trigger` and missing both `Evidence`+`Rationale` fail loudly; invalid statuses (`done`, `closed`, `fixed`, `deferred-again`, `Open`, `OPEN`) rejected; near-miss IDs (`12x4-RV6`, `112.4-RV6`, `12.4-RV60`) do not collide with `12.4-RV6`; baseline entries without `Test` field fail loudly; multi-segment test names rejected. Added a `RealRepo` test that parses the real `deferred-work.md` and asserts no open structured S11-F* baseline entries currently exist.
- Added a "Sprint Status History Conventions" section to `CONTRIBUTING.md` codifying the short-dated-breadcrumb rule and pointing detailed evidence at story artifacts, deferred-work entries, run logs, or review documents.
- Added an "Epic 14 Story Close-out Rules" section to `CONTRIBUTING.md` that requires every Epic 14 story to enumerate targeted deferred IDs in completion notes, mark each `resolved`/`accepted`/`carried-forward`, refresh `Re-open trigger` on entries left open, and forbids root-level submodule pointer changes by default.
- Did not initialize, update, or bump nested submodules. No `.github/`, `src/`, runtime tests, release tooling, or package metadata files were touched.

#### Code Review Close-out 2026-05-04

- Applied 3/3 review patches.
- `ParseStructuredDeferredEntries` now fails loudly when a recognized structured field appears before `ID:`, closing the missing-ID silent-ignore gap.
- `FlushPendingStructuredEntry` now enforces the status-specific schema: `resolved` requires `Evidence:`, while `accepted` and `carried-forward` require `Rationale:`.
- Added four focused parser fixtures: missing `ID`, resolved-without-evidence, accepted-without-rationale, and carried-forward-without-rationale.
- Removed untracked `Hexalith.Memories.sln` so repository-scope validation again contains only the allowed story files.

#### Validation Evidence 2026-05-04

- `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~CiTestInventoryTests"` — **PASS 40/40**, 0 failed, 0 skipped (was 25 tests before this story; net +15 across the new structured-field fixture suite, replacing the 3 substring-era fixture tests).
- `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj` — **PASS 363/363**, 0 failed, 0 skipped (no regressions in the broader CLI test surface).
- `git diff --check -- _bmad-output/implementation-artifacts/deferred-work.md tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs CONTRIBUTING.md _bmad-output/implementation-artifacts/14-5-deferred-register-governance-and-sprint-status-hygiene.md _bmad-output/implementation-artifacts/sprint-status.yaml` — **clean** (no whitespace conflict markers).
- Repository scope confirmed: SDK `10.0.201` resolved from `%TEMP%/dotnet-sdk-10.0.201` (story 14.4 carry-over); `git status` shows changes only under the allowed file set plus the story artifact and `sprint-status.yaml`. No `.github/`, `src/`, runtime tests, release scripts, package metadata, or submodule pointers changed.
- Post-review close-out: `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~CiTestInventoryTests"` with `DOTNET_ROOT=%TEMP%/dotnet-sdk-10.0.201` — **PASS 44/44**, 0 failed, 0 skipped. First rerun hit stale `testhost` process `77284` holding the test DLL; stopping that stale process allowed the focused lane to pass.

#### Targeted Deferred IDs Disposition

- `12.4-RV6` — **resolved**. Evidence: structured-field parser landed in `CiTestInventoryTests`; new fixture `ReadOpenDeferredBaselines_NarrativeMentionsBaseline_NotMisclassified` proves prose substrings no longer drive classification.
- `12.4-RV19` — **resolved**. Evidence: `DeferredKeyRegex` deleted from the deferred-work parser path; `StructuredFieldRegex` + `StructuredIdShape` accept any schema-conformant ID without requiring a literal trailing period; new theory `ReadOpenDeferredBaselines_StructuredEntryWithSimilarId_DoesNotCountAsTargetId` covers the boundary cases.
- `12.6-RV2` — **resolved**. Evidence: closed alongside `12.4-RV6`; classification is now driven by `Target artifact == "tools/test-release.ps1"` rather than substring scans for `baseline`/`release lane`/`test-release.ps1` in entry prose.
- `13.7-RV5` — **resolved**. Evidence: `CONTRIBUTING.md` "Sprint Status History Conventions" section pins the forward-looking rule (short dated breadcrumbs that link out to story artifacts, deferred-work IDs, run logs, or review docs); historical Epic 1-13 status lines are intentionally not rewritten.

### File List

- `_bmad-output/implementation-artifacts/14-5-deferred-register-governance-and-sprint-status-hygiene.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `CONTRIBUTING.md`

### Party-Mode Review

- Date/time: `2026-05-04T15:03:24+02:00`
- Selected story key: `14-5-deferred-register-governance-and-sprint-status-hygiene`
- Command/skill invocation used: `/bmad-party-mode 14-5-deferred-register-governance-and-sprint-status-hygiene; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - The deferred-register schema needed an exact Markdown field contract before parser work starts.
  - The status vocabulary needed clear semantics so `resolved`, `accepted`, and `carried-forward` cannot become synonyms.
  - The story needed stronger scope boundaries to avoid broad migration of the historical deferred register.
  - Parser tests needed positive and negative fixtures for field parsing, exact dotted IDs, invalid statuses, and prose false positives.
  - Sprint-status hygiene needed a concrete forward-looking rule and repository-scope validation needed explicit no-forbidden-file/no-submodule evidence.
- Changes applied:
  - Added `Party-Mode Review Clarifications - 2026-05-04` covering canonical field labels, closed status semantics, targeted migration scope, field-aware parser expectations, sprint-status note guidance, optional checker constraints, and forbidden-path/submodule guardrails.
  - Expanded testing probes to include missing-field, invalid-status, exact-ID, and repository-scope validation evidence.
- Findings deferred:
  - Full historical deferred-register migration remains out of scope and should only be considered in a separate governance cleanup story if maintainers decide the planning value justifies the churn.
  - Adding a standalone checker remains optional; it should be used only if it stays small and clearer than focused C# parser tests.
- Final recommendation: `ready-for-dev`

### Change Log

- 2026-05-03: Created Story 14.5 and promoted it from `backlog` to `ready-for-dev`.
- 2026-05-04: Party-mode review completed; added pre-dev schema, parser, status-semantics, sprint-status, and scope guardrail clarifications.
- 2026-05-04: Dev-story implementation completed. Added schema-for-active-entries section and Story 14.5 close-out rollup to `deferred-work.md`; migrated 12.4-RV6, 12.4-RV19, 12.6-RV2, and 13.7-RV5 to structured field blocks marked `Status: resolved`; replaced substring-driven baseline classifier in `CiTestInventoryTests.cs` with structured-field parser plus 18 fixture tests covering positive, negative, status-vocabulary, missing-field, exact-ID, and `Test:` requirements; codified sprint-status history conventions and Epic 14 story close-out rules in `CONTRIBUTING.md`. Validation: `CiTestInventoryTests` 40/40 PASS, full `Hexalith.Memories.Cli.Tests` 363/363 PASS, `git diff --check` clean across the allowed file set. Story moved `in-progress → review`.
- 2026-05-04: Code-review close-out applied 3/3 patches. Missing-ID structured fields now fail closed, status-specific `Evidence:` / `Rationale:` semantics are enforced, untracked project metadata was removed from the working tree, and focused validation passed `CiTestInventoryTests` 44/44. Story moved `review → done`.

## Story Completion Status

Story 14.5 completed and reviewed. Status set to `done`.
