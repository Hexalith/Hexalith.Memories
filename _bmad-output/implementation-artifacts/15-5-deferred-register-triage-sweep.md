# Story 15.5: Deferred Register Triage Sweep

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want a bounded sweep of remaining deferred entries,
so that historical noise, consciously accepted risks, and true backlog candidates are separated before the next implementation epic.

## Acceptance Criteria

1. Given `deferred-work.md` still contains historical prose entries, when this story runs, then entries selected for active planning are migrated to the Story 14.5 structured schema without bulk-rewriting unrelated history.

2. Given some remaining items are quality hardening rather than architectural decisions, when the sweep selects candidates, then items such as `12.4-RV20`, `12.6-RV5`, and `Story-9.3-ProjectionRegistryCrossCheck` are either promoted into named future stories, accepted with rationale, or carried forward with refreshed triggers.

3. Given the Epic 14 retrospective named carry-forward risks, when the sweep completes, then the retrospective's remaining-risk list is reconciled with the current deferred register, including the already-resolved `13.7-RV4` repository-root helper cleanup.

4. Given the backlog should remain actionable, when this story closes, then it proposes no more than five follow-up stories, each with explicit deferred IDs, target artifacts, and validation expectations.

## Tasks / Subtasks

- [x] Task 0 - Establish the sweep baseline before editing (AC: 1-4)
  - [x] Read `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/implementation-artifacts/epic-14-retro-2026-05-12.md`, `_bmad-output/planning-artifacts/epics.md`, and the current Story 15.1 through Story 15.4 artifacts before editing.
  - [x] Confirm Stories 15.1, 15.2, 15.3, and 15.4 are not actively `in-progress` or `review`; if one is active, do not rewrite or disposition its targeted IDs in this story.
  - [x] Build a short inventory of open, accepted, and carried-forward entries that remain prose-only or stale after Story 14.5. Treat the Story 14.5 structured schema as the source of truth for active entries.
  - [x] Explicitly mark `13.7-RV4` as already reconciled because the current deferred register has a resolved structured block dated 2026-05-12.

- [x] Task 1 - Select a bounded candidate set (AC: 1, 2, 4)
  - [x] Select only entries that need active planning signal, stale trigger refresh, or an explicit risk decision. Do not bulk-migrate every historical prose entry.
  - [x] Include at least the named candidates unless analysis proves one is already resolved or owned elsewhere: `12.4-RV20`, `12.6-RV5`, and `Story-9.3-ProjectionRegistryCrossCheck`.
  - [x] Check whether release/provider/migration/token risks named by the Epic 14 retrospective are already owned by Story 15.1, 15.2, 15.3, or 15.4. If they are owned there, cross-reference rather than duplicating them.
  - [x] Keep the selected set small enough that the story can close with no more than five follow-up story proposals.

- [x] Task 2 - Apply structured deferred-work dispositions (AC: 1, 2, 3)
  - [x] Add a Story 15.5 rollup section to `_bmad-output/implementation-artifacts/deferred-work.md`.
  - [x] For each selected entry, add the Story 14.5 structured fields: `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, and either `Evidence` or `Rationale`.
  - [x] Use `carried-forward` only when a named future story or trigger owns the risk. Use `accepted` only when the team is intentionally keeping the risk without planned implementation.
  - [x] Preserve historical prose entries below their original headings. Do not delete old context unless it is duplicated by the new structured rollup and deletion is needed to prevent conflicting active status.
  - [x] Do not mark a risk `resolved` without repository evidence such as code, test, documentation, or a prior story record.

- [x] Task 3 - Reconcile Epic 14 retrospective carry-forward list (AC: 3)
  - [x] For `S11-FC`, `13.2-RV4`, `13.1-RV6` and related provider work, and `13.6-RV1`, record that the current Epic 15 backlog already has dedicated ready stories: 15.1, 15.4, 15.2, and 15.3 respectively.
  - [x] For `13.7-RV4`, point to the current resolved structured entry in `deferred-work.md` and do not recreate a new backlog item.
  - [x] If the retrospective's "Preparation For The Next Work" section is now stale because Epic 15 exists, record the reconciliation in this story artifact or the deferred-work rollup rather than editing the retrospective unless a maintainer explicitly wants retro history rewritten.

- [x] Task 4 - Propose no more than five follow-up stories (AC: 2, 4)
  - [x] Group selected entries by coherent implementation surface, not by chronology. Prefer one story per real ownership boundary.
  - [x] Each proposed follow-up must name explicit deferred IDs, target artifacts, and validation expectations.
  - [x] Suggested proposal buckets to consider:
    - strict release-baseline replay evidence for `12.4-RV20`;
    - telemetry test isolation hardening for `12.6-RV5`;
    - projection-registry cross-check design for `Story-9.3-ProjectionRegistryCrossCheck`;
    - handler-observation fidelity and Tier-2 proof for related Story 9.3 entries, only if selected;
    - author-experience cleanup for remaining story-scope diagnostic nits, only if selected.
  - [x] Do not add the proposed stories directly to `epics.md` or `sprint-status.yaml` unless the project lead explicitly approves converting the proposals into backlog.

- [x] Task 5 - Validate governance changes (AC: 1-4)
  - [x] Run or inspect the focused deferred-work parser coverage that Story 14.5 introduced: `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~CiTestInventoryTests"`, if the local SDK and time budget permit it.
  - [x] If no code parser changed, at minimum run `git diff --check -- _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/15-5-deferred-register-triage-sweep.md`.
  - [x] Manually verify every new structured block contains exactly one `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, and the required `Evidence` or `Rationale`.
  - [x] Record the final proposal list and any validation limits in this story's Dev Agent Record.

## File Scope

Allowed files for this story:

- `_bmad-output/implementation-artifacts/deferred-work.md` - UPDATE. Add the Story 15.5 bounded triage rollup, structured dispositions, carry-forward/accepted rationale, and no-more-than-five follow-up story proposals.
- `_bmad-output/implementation-artifacts/15-5-deferred-register-triage-sweep.md` - UPDATE. Record implementation notes, proposal list, validation, review findings, and file list.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad workflow/status transitions.

Possible files only if analysis proves they are necessary:

- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` - UPDATE only if the structured deferred-work parser cannot read a necessary Story 15.5 schema case.
- `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs` - UPDATE only if this story chooses to directly resolve `12.6-RV5` instead of carrying it forward; otherwise leave for a future implementation story.
- `tools/check-story-file-scope.py` - UPDATE only if this story chooses to directly resolve selected story-scope author-experience nits; otherwise leave for a future implementation story.

Read/verify only:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-12.md`
- `_bmad-output/implementation-artifacts/epic-14-retro-2026-05-12.md`
- `_bmad-output/implementation-artifacts/14-5-deferred-register-governance-and-sprint-status-hygiene.md`
- `_bmad-output/implementation-artifacts/15-1-release-edge-case-preflight-hardening.md`
- `_bmad-output/implementation-artifacts/15-2-provider-model-dimension-registry.md`
- `_bmad-output/implementation-artifacts/15-3-live-migration-coordination-policy.md`
- `_bmad-output/implementation-artifacts/15-4-token-endpoint-transport-policy.md`
- `CONTRIBUTING.md`
- `docs/dev/release-runbook.md`
- `docs/operations/embedding-providers.md`

Forbidden by default:

- `.github/**`
- `src/**`
- `tests/**/*.cs` except the possible files listed above
- `tools/**` except `tools/check-story-file-scope.py` when selected explicitly
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-12.md`
- `Directory.Packages.props`
- `Directory.Build.props`
- `NuGet.config`
- `Hexalith.AI.Tools/**`
- `Hexalith.Commons/**`
- `Hexalith.EventStore/**`
- Any submodule pointer change

## Dev Notes

### Current Implementation State

`deferred-work.md` now has a Story 14.5 structured schema for active entries. The top of the file documents the required fields and closed entries such as `13.7-RV4`, `12.4-RV6`, `12.4-RV19`, `12.6-RV2`, and `13.7-RV5` already use that schema. Historical prose entries remain valid context but should not be treated as machine-readable active status unless this story migrates them.

Epic 15 already converted the largest Epic 14 carry-forward risks into ready story artifacts:

- `S11-FC`, `12.1-RV3`, and `12.1-RV4` are owned by Story 15.1.
- `13.1-RV6`, `13.1-RV10`, `13.1-RV11`, and `13.3-RV8` are owned by Story 15.2.
- `13.6-RV1` and `13.6-RV3` are owned by Story 15.3.
- `13.2-RV4` is owned by Story 15.4.
- `13.7-RV4` is already resolved in the current deferred register by the AppHost-owned `RepositoryRootLocator`.

This story should not duplicate those implementation stories. Its value is to separate the remaining register noise from real backlog candidates before another implementation epic is planned.

### Named Candidate Entries

The Epic 15 source names these candidates explicitly:

- `12.4-RV20`: strict literal per-SHA replay drill for historical release/test evidence. This is quality evidence, not a runtime feature. It probably belongs in a future release-quality proof story unless the team accepts the current HEAD-inheritance rationale.
- `12.6-RV5`: `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` still uses fixed tenant id `"t"` and a non-thread-safe capture list. This is a focused test-isolation hardening candidate.
- `Story-9.3-ProjectionRegistryCrossCheck`: handler mismatch detection validates routing config rather than the actual projection registry bound by tenant application code. This is an architectural design candidate and should not be patched casually inside a governance sweep.

Other likely candidates may exist around Story 9.3 handler observation fidelity, story-scope author experience (`12.4-RV10`, `12.4-RV11`), and test fixture hardening (`12.6-RV1`, `12.6-RV3`, `12.6-RV4`). Select them only when they produce a coherent proposal set under the five-story limit.

### Structured Disposition Guidance

Use these statuses carefully:

- `resolved` requires evidence already present in the repository.
- `accepted` means the team intentionally keeps the risk with rationale and no planned follow-up.
- `carried-forward` means a named future story, artifact, or trigger owns the risk.

Do not change a status just to reduce open count. The output of this story should be a cleaner planning queue, not a falsely green register.

### Follow-Up Proposal Shape

Each proposed follow-up story should include:

- proposed story title;
- deferred IDs included;
- target artifacts;
- validation expectations;
- one-sentence scope boundary.

The story should propose no more than five follow-ups. It should not add those stories to `epics.md` or `sprint-status.yaml` unless that conversion is requested separately.

### Implementation Guardrails

- Preserve historical context in `deferred-work.md`; add a Story 15.5 rollup rather than rewriting the file top to bottom.
- Keep sprint-status updates concise. Detailed evidence belongs in the story artifact and deferred-work rollup.
- Do not edit dirty planning artifacts as part of this story unless explicitly asked. This run started with existing dirty planning-artifact changes outside the BMAD story-operation path.
- Do not initialize or update nested submodules. Do not change root-level submodule pointers.

### Testing Requirements

Minimum validation before review:

```powershell
git diff --check -- _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/15-5-deferred-register-triage-sweep.md
```

Run this if the deferred-work parser or CI inventory contract changes:

```powershell
dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~CiTestInventoryTests"
```

Manual validation is required even when no code changes: every new structured entry must include the required Story 14.5 fields and must not conflict with an older active entry for the same ID.

## Project Structure Notes

This is a governance and planning-hygiene story. The expected implementation stays in the deferred register and this story artifact. Runtime source, release workflows, migration tooling, provider validation, OIDC token transport, broad tests, and planning artifact rewrites are out of scope unless a selected entry is intentionally resolved inside this story and the story records why.

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 15 and Story 15.5 acceptance criteria.
- `_bmad-output/implementation-artifacts/deferred-work.md` - target register, Story 14.5 structured schema, historical prose entries, and already-resolved `13.7-RV4`.
- `_bmad-output/implementation-artifacts/epic-14-retro-2026-05-12.md` - carry-forward risk list and action items that Story 15.5 must reconcile.
- `_bmad-output/implementation-artifacts/14-5-deferred-register-governance-and-sprint-status-hygiene.md` - schema origin and governance conventions.
- `_bmad-output/implementation-artifacts/15-1-release-edge-case-preflight-hardening.md` - current release-risk story ownership.
- `_bmad-output/implementation-artifacts/15-2-provider-model-dimension-registry.md` - current provider-registry story ownership.
- `_bmad-output/implementation-artifacts/15-3-live-migration-coordination-policy.md` - current migration-coordination story ownership.
- `_bmad-output/implementation-artifacts/15-4-token-endpoint-transport-policy.md` - current token-transport story ownership.
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` - structured deferred-work parser tests if schema behavior changes.
- `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs` - referenced by `12.6-RV5` if selected for direct hardening.
- `tools/check-story-file-scope.py` - referenced by remaining story-scope diagnostic nits if selected.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Pre-dev hardening preflight JSON timestamp `2026-05-12T19:02:57Z` failed only `working tree cleanliness`.
- The failed check's stdout was ` M _bmad-output/planning-artifacts/epics.md\n M _bmad-output/planning-artifacts/implementation-readiness-report-2026-05-12.md\n`; both dirty paths are planning artifacts outside BMAD story-operation paths, so the run continued with a soft working-tree warning and left those changes untouched.
- Story selection chose `15-5-deferred-register-triage-sweep` because `ready_count` was `4`, below the target of `5`, and this was the first backlog story in sprint-status order.
- `/bmad-create-story 15-5-deferred-register-triage-sweep` context gathering loaded Epic 15 planning, sprint status, root project context, Story 15.1 through 15.4 artifacts, the Story 14.5 lessons/schema baseline, Epic 14 retrospective, current deferred-work entries, the May 12 implementation-readiness report, and recent git history.
- No external technology research was needed for this story. The implementation surface is repository-owned governance, deferred-register triage, and planning hygiene.
- Moved Story 15.5 from `ready-for-dev` to `in-progress` on 2026-05-15 before editing, then to `review` after validation.
- Validation initially hit a stale `testhost` process (`100128`) locking CLI test binaries; stopping that process cleared the local build/test blockage.
- `git diff --check -- _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/15-5-deferred-register-triage-sweep.md` passed with only expected LF-to-CRLF working-copy warnings.
- Manual structured-block validation passed for five Story 15.5 dispositions: each block has exactly one `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, and `Rationale`.
- `dotnet test tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --filter "FullyQualifiedName~CiTestInventoryTests"` passed: 48 passed, 0 failed, 0 skipped.

### Completion Notes List

- Story context created on 2026-05-12.
- Scope is limited to bounded deferred-register triage, structured dispositions for selected entries, Epic 14 risk reconciliation, and no-more-than-five follow-up story proposals.
- Added a Story 15.5 triage rollup to `deferred-work.md` for five selected entries: carried forward `12.4-RV20`, `12.6-RV5`, and `Story-9.3-ProjectionRegistryCrossCheck`; accepted `12.4-RV10` and `12.4-RV11`.
- Reconciled Epic 14 retrospective carry-forward risks to existing Story 15.1 through 15.4 ownership and the already-resolved `13.7-RV4` deferred-register block, without rewriting retrospective history.
- Proposed three bounded follow-up stories: `Strict Release Baseline Replay Evidence`, `Telemetry Test Isolation Hardening`, and `Projection Registry Cross-Check Design`.
- Did not add proposed stories to `epics.md` or `sprint-status.yaml`; they remain proposals in the deferred register until the project lead explicitly converts them into backlog.
- Existing dirty planning-artifact changes were not touched.
- No submodule state was touched.

### File List

- `_bmad-output/implementation-artifacts/15-5-deferred-register-triage-sweep.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-12: Created Story 15.5 and promoted it from `backlog` to `ready-for-dev`.
- 2026-05-15: Started Story 15.5 implementation and moved status to `in-progress`.
- 2026-05-15: Added bounded deferred-register triage rollup, reconciled Epic 14 carry-forward risks, recorded three follow-up proposals, validated governance changes, and moved status to `review`.

## Story Completion Status

Implemented and ready for review. Story 15.5 added the deferred-register triage rollup, recorded accepted/carried-forward dispositions, reconciled Epic 14 carry-forward risks to existing ownership, proposed three follow-up stories without converting them to backlog, and passed focused validation.
