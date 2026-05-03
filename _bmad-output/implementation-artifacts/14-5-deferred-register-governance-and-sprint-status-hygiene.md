# Story 14.5: Deferred Register Governance and Sprint-Status Hygiene

Status: ready-for-dev

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

- [ ] Task 1 - Define the deferred-work entry schema and migration rule (AC: 1, 2, 4)
  - [ ] Add a short schema guide near the top of `_bmad-output/implementation-artifacts/deferred-work.md` that defines required fields for active entries: `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, and either `Evidence` or `Rationale`.
  - [ ] Use the allowed status vocabulary `open`, `resolved`, `accepted`, and `carried-forward`. Do not introduce near-synonyms such as `done`, `closed`, `fixed`, or `deferred-again`.
  - [ ] Preserve historical prose where it carries useful context, but add structured field lines so tools do not need to infer status or classification from arbitrary paragraph text.
  - [ ] State that completed or accepted entries must retain enough evidence to explain why the risk is no longer open.
  - [ ] Keep the schema Markdown-readable. Do not replace the register with JSON/YAML unless explicitly approved.

- [ ] Task 2 - Migrate the Story 14.5 target deferred IDs (AC: 1, 2, 4)
  - [ ] Update `12.4-RV6` so the baseline/filter parser risk has structured fields and points to the new parser contract in `CiTestInventoryTests`.
  - [ ] Update `12.4-RV19` so `DeferredKeyRegex` format brittleness is either resolved by parser changes or carried forward with the exact accepted key grammar and trigger.
  - [ ] Update `13.7-RV5` so sprint-status long-line history is either resolved by the new guidance/tooling or carried forward with a concrete trigger.
  - [ ] Revisit adjacent `12.6-RV2` because it explicitly says it realizes `12.4-RV6`; close, accept, or carry it forward consistently with the new schema.
  - [ ] Do not bulk-migrate all 266 historical entries. Convert only the Story 14.5 target set plus a tiny representative fixture section if tests require it.

- [ ] Task 3 - Replace deferred-work prose heuristics in CI inventory tests (AC: 4)
  - [ ] Update `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` so deferred baseline parsing reads structured fields instead of classifying entries by substring checks such as `baseline`, `test-release.ps1`, or `release lane`.
  - [ ] Replace `DeferredKeyRegex` with a parser that accepts the documented structured ID field and does not depend on a literal period after `S11-F*`.
  - [ ] Keep closed/resolved entries out of the active baseline set by reading the structured status field instead of looking for `[resolved`, `[closed`, or `[done]` in the heading text.
  - [ ] Add fixture tests for `open`, `resolved`, `accepted`, and `carried-forward` entries, including a migrated S11-F baseline entry and a non-baseline deferred item.
  - [ ] Keep the zero-accepted-release-filter behavior intact when no active baseline entries claim a release filter.

- [ ] Task 4 - Add sprint-status history guidance without rewriting history (AC: 3, 5)
  - [ ] Add concise guidance to `CONTRIBUTING.md` or another existing contributor/process document that says sprint-status updates should use short dated notes and avoid appending multi-sentence narratives to a single YAML line.
  - [ ] Recommend putting detailed evidence in the story artifact, `deferred-work.md`, or a dedicated review/retro document, then linking or naming that artifact from `sprint-status.yaml`.
  - [ ] Do not rewrite completed Epic 1-13 story history comments as part of this story. Historical cleanup is out of scope unless a parser failure proves a targeted edit is required.
  - [ ] If a helper script is added, keep it advisory or validation-focused. It must not auto-reformat sprint status without an explicit maintainer command.

- [ ] Task 5 - Update Epic 14 bookkeeping rules for future story close-out (AC: 2, 3, 5)
  - [ ] Add or update guidance that Epic 14 implementation stories must list every targeted deferred ID in completion notes and mark it `resolved`, `accepted`, or `carried-forward`.
  - [ ] Make clear that discussing an item is not closure. Closure requires code, test, documentation, or explicit acceptance evidence.
  - [ ] Record any entries intentionally left open with a fresh `Re-open trigger` and rationale rather than silently removing them.
  - [ ] Keep root-level submodule pointer changes forbidden by default and never initialize/update nested submodules for this story.

- [ ] Task 6 - Validate the governance lane (AC: 1-5)
  - [ ] Run `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~CiTestInventoryTests"`.
  - [ ] Run `git diff --check -- _bmad-output/implementation-artifacts/deferred-work.md tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs CONTRIBUTING.md _bmad-output/implementation-artifacts/14-5-deferred-register-governance-and-sprint-status-hygiene.md`.
  - [ ] If a new tooling test harness is added, run its focused command and record the exact command/output summary in the Dev Agent Record.
  - [ ] Record before/after counts for the targeted deferred IDs only. Do not report broad deferred-register cleanup unless it was actually performed and scoped.

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

### File List

- `_bmad-output/implementation-artifacts/14-5-deferred-register-governance-and-sprint-status-hygiene.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-03: Created Story 14.5 and promoted it from `backlog` to `ready-for-dev`.

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created. Status set to `ready-for-dev`.
