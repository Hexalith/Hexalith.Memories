---
title: 'Preserve Epic 23 ingestion invariants in Epic 24 and Epic 25 reviews'
type: 'bugfix'
created: '2026-08-02'
status: 'done'
review_loop_iteration: 2
baseline_commit: '8d0c1a5873d65e2ead93f50e84cd7ce20c99a92e'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epic 23 required Epic 24 and Epic 25 reviews to preserve six ingestion invariants, but Epic 24 recorded only a partial narrative and Epic 25 omitted the carry-forward. The completed epics therefore lack an explicit, auditable checklist even though the current implementation still preserves the behavior.

**Approach:** Add the same six-row gate to both canonical epic definitions and contexts, then append dated corrective evidence matrices to both retrospectives. Preserve the original historical record and distinguish current-tree verification from evidence that existed during the original reviews.

## Boundaries & Constraints

**Always:** Preserve all existing retrospective text and statuses; cover claim-check payloads, captured workflow configuration, chunked semantic vectors, source-payload retention, tenant index readiness, and single-operation rate-limit admission as independent rows; attach rerunnable commands, reviewer/date, and pass/fail/blocked verdicts; describe the 2026-08-02 results as corrective verification against `main@8d0c1a5873d65e2ead93f50e84cd7ce20c99a92e`.

**Ask First:** Any production-code change, story or epic reopening, sprint-status edit, changed invariant definition, or expansion beyond the six approved documentation artifacts.

**Never:** Claim that every original Epic 24/25 story review ran the checklist; silently rewrite the Epic 24 `In progress` row or conceal the Epic 25 omission; use broad-suite/build-only evidence in place of the focused invariant tests; modify PRD, architecture, UX, source, tests, deployment, dependencies, submodules, or release scope.

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md` -- approved scope, exact invariant wording, evidence commands, and audit rationale.
- `_bmad-output/planning-artifacts/epics.md` -- canonical Epic 24 and Epic 25 definitions where the shared review gate belongs.
- `_bmad-output/implementation-artifacts/epic-24-context.md` and `_bmad-output/implementation-artifacts/epic-25-context.md` -- execution/review contexts requiring independently reviewable checklist rows.
- `_bmad-output/implementation-artifacts/epic-24-retro-2026-07-06.md` -- partial historical carry-forward plus corrective evidence addendum.
- `_bmad-output/implementation-artifacts/epic-25-retro-2026-07-12.md` -- omitted historical carry-forward plus corrective evidence addendum.
- `_bmad-output/implementation-artifacts/23-7-index-provisioning-ownership.md` -- canonical readiness contract: ingestion cannot create indexes on demand, but known additive TAG-field upgrades are explicitly preserved.
- `src/Hexalith.Memories.Server/{Ingestion,Workflows,Activities,Infrastructure}` and `tests/Hexalith.Memories.Server.Tests` -- current implementation and focused regression evidence for the six verdicts.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/planning-artifacts/epics.md` -- add the identical six-invariant review gate beneath both Epic 24 and Epic 25 metadata; express readiness as no on-demand index creation while preserving Story 23.7's approved additive TAG-field upgrades; scope single-operation admission to Story 23.5 ingestion embedding activities.
- [x] `_bmad-output/implementation-artifacts/epic-24-context.md` and `_bmad-output/implementation-artifacts/epic-25-context.md` -- add six-row review checklists before cross-story dependencies, link each corrective status to its retrospective addendum, align readiness wording with Story 23.7, and preserve the Story 23.5 ingestion boundary.
- [x] `_bmad-output/implementation-artifacts/epic-24-retro-2026-07-06.md` and `_bmad-output/implementation-artifacts/epic-25-retro-2026-07-12.md` -- append dated matrices that identify the original gap, cite complete static/executable evidence, identify the Codex review through this spec artifact plus Administrator approval, and record one corrective verdict per invariant.
- [x] `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md` -- retain the approved proposal as the audit record; align Story 23.1/23.5/23.7 wording, the complete named-class evidence lane, reviewer attribution, structural verification, tracked-baseline language, and accounting for six handoff artifacts plus this workflow control spec.

**Acceptance Criteria:**
- Given the canonical Epic 24 and Epic 25 definitions, when their pre-story review gates are compared, then both contain the same six invariant obligations and evidence/N/A/blocker rules; readiness forbids on-demand index creation without contradicting approved additive TAG-field upgrades.
- Given either epic context, when a reviewer opens the review checklist, then all six invariants have separate evidence expectations and dated corrective statuses linked to the matching retrospective.
- Given either retrospective, when its original record and addendum are inspected, then the original text remains intact, the Epic 24 partial record or Epic 25 omission is explicit, and six current-tree verdicts are tied to rerunnable evidence.
- Given the focused invariant class list, when the built xUnit v3 assembly runs, then every selected contract, claim-check, determinism, readiness, ownership, retention, and admission test passes with zero failures and zero skips.
- Given the completed change, when repository status and diff are inspected, then only the five approved target documents, the approved proposal, and this workflow control spec are changed; the proposal distinguishes its six handoff artifacts from the seventh workflow control artifact, while `sprint-status.yaml`, product code, tests, and submodule pointers remain untouched.

## Spec Change Log

- Review loop 1 — adversarial review found that the readiness wording contradicted Story 23.7's approved additive TAG-field upgrades, the 106-test project filter omitted the exact chunk-key/extracted-text/determinism/ownership evidence and used a disfavored xUnit v3 route, and artifact accounting omitted the workflow control spec. Amended the Code Map, tasks, acceptance criteria, and verification commands to preserve the no-on-demand-create boundary, run complete named classes through the built xUnit v3 assembly, verify product inputs still equal the pinned baseline, cover untracked-file whitespace, and distinguish six approved handoff artifacts from the seventh workflow spec. Known-bad states avoided: a false I5 pass against `FT.ALTER`, green evidence while cited contract tests are red or unselected, and a misleading six-file diff claim. KEEP: additive-only documentation changes; identical Epic 24/25 gates; six independent checklist rows; original retrospective text/statuses; explicit Epic 24 partial record and Epic 25 omission; dated current-tree evidence that never claims every original review executed the checklist.
- Review loop 2 — adversarial review found that the admission row overreached beyond Story 23.5's ingestion activities, the executable lane omitted the single-text/config-cache/actor admission contract, the chunk row did not execute Story 23.1's base-identity retrieval/cleanup/consistency proofs, and the readiness row did not execute all indexing/provisioning-owner seams. Amended the non-frozen plan to restore the original Story 23.1, 23.5, and 23.7 boundaries; expand the named-class lane; identify the reviewer through this stable spec artifact; narrow baseline claims to tracked `src`/`tests`; and add fail-closed structural/whitespace checks. Known-bad states avoided: applying ingestion admission claims to search/migration/restore, a false I3 pass on key construction alone, a false I5 pass on only one indexing activity, and a piped no-index command that masks its expected exit status. KEEP: the frozen six-invariant intent and boundaries; additive-only documentation changes; identical Epic 24/25 gates; original retrospective text/statuses; explicit historical gaps; current-tree corrective verdicts only.
- Final review patches — narrowed I3 to the executable storage/base-ID parsing/semantic-result deduplication proof because the approved documentation-only scope forbids adding the missing cleanup/consistency/repair/migration/graph-scope tests; serialized the focused xUnit lane with `-parallel none`; chained baseline/build/test preconditions fail-closed; replaced physical-line gate comparison and aggregate row/link counts with semantic, per-invariant, per-epic assertions; and captured no-index status plus diagnostics separately so expected new-file differences cannot mask whitespace errors.

## Verification

**Commands:**
- `test "$(rg -c -F 'Epic 23 ingestion invariant review gate (corrected 2026-08-02)' _bmad-output/planning-artifacts/epics.md)" = 2 && test "$(awk '/^## Epic 24:/{epic_scope=1; next} /^## Epic 25:/{epic_scope=0} epic_scope && /^\*\*Epic 23 ingestion invariant review gate/{gate_scope=1} gate_scope{print} gate_scope && /^Each verdict records/{exit}' _bmad-output/planning-artifacts/epics.md | rg -c -F 'Epic 23 ingestion invariant review gate')" = 1 && test "$(awk '/^## Epic 25:/{epic_scope=1; next} /^## Epic 26:/{epic_scope=0} epic_scope && /^\*\*Epic 23 ingestion invariant review gate/{gate_scope=1} gate_scope{print} gate_scope && /^Each verdict records/{exit}' _bmad-output/planning-artifacts/epics.md | rg -c -F 'Epic 23 ingestion invariant review gate')" = 1 && diff -u <(awk '/^## Epic 24:/{epic_scope=1; next} /^## Epic 25:/{epic_scope=0} epic_scope && /^\*\*Epic 23 ingestion invariant review gate/{gate_scope=1} gate_scope{print} gate_scope && /^Each verdict records/{exit}' _bmad-output/planning-artifacts/epics.md) <(awk '/^## Epic 25:/{epic_scope=1; next} /^## Epic 26:/{epic_scope=0} epic_scope && /^\*\*Epic 23 ingestion invariant review gate/{gate_scope=1} gate_scope{print} gate_scope && /^Each verdict records/{exit}' _bmad-output/planning-artifacts/epics.md)` -- expected: exit `0`; exactly one non-empty gate is under each named epic and the two extracted gates are byte-identical.
- `for context_doc in _bmad-output/implementation-artifacts/epic-24-context.md _bmad-output/implementation-artifacts/epic-25-context.md; do test "$(rg -c -F '## Review Checklist — Epic 23 Ingestion Invariants' "$context_doc")" = 1 || exit 1; for invariant_name in 'Claim-check workflow payloads' 'Captured workflow configuration' 'Chunked semantic vectors' 'Source-payload retention' 'Tenant index readiness' 'Single-operation admission'; do test "$(rg -c "^\\| $invariant_name \\|" "$context_doc")" = 1 || exit 1; done; done` -- expected: exit `0`; each context has one heading and exactly one row for each named invariant.
- `for retro_doc in _bmad-output/implementation-artifacts/epic-24-retro-2026-07-06.md _bmad-output/implementation-artifacts/epic-25-retro-2026-07-12.md; do test "$(rg -c -F '## Corrective Review Addendum — Epic 23 Ingestion Invariants (2026-08-02)' "$retro_doc")" = 1 || exit 1; for row_id in I1 I2 I3 I4 I5 I6; do test "$(rg -c "^\\| $row_id \\|" "$retro_doc")" = 1 || exit 1; done; done` -- expected: exit `0`; each retrospective has one addendum and exactly one row for each invariant ID.
- `test "$(rg -c -F 'Correctively passed 2026-08-02; see the [Epic 24 retrospective addendum](epic-24-retro-2026-07-06.md).' _bmad-output/implementation-artifacts/epic-24-context.md)" = 6 && test "$(rg -c -F 'Correctively passed 2026-08-02; see the [Epic 25 retrospective addendum](epic-25-retro-2026-07-12.md).' _bmad-output/implementation-artifacts/epic-25-context.md)" = 6` -- expected: exit `0`; every status links to its own epic's addendum.
- `git diff --quiet 8d0c1a5873d65e2ead93f50e84cd7ce20c99a92e -- src tests && test -z "$(git status --short --untracked-files=all -- src tests)"` -- expected: tracked product/test inputs match the pinned corrective-review baseline and no untracked product/test inputs exist.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false` -- expected: build succeeds and restores required assets.
- `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -class Hexalith.Memories.Server.Tests.Ingestion.IngestionPayloadClaimCheckTests -class Hexalith.Memories.Server.Tests.Ingestion.DaprIngestionWorkflowSchedulerTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.ExtractContentActivityTests -class Hexalith.Memories.Server.Tests.Ingestion.ContentChunkerTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateChunkEmbeddingsActivityTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateEmbeddingActivityTests -class Hexalith.Memories.Server.Tests.Activities.Ingestion.GenerateEmbeddingActivityConfigTests -class Hexalith.Memories.Server.Tests.Ingestion.TenantEmbeddingConfigProviderTests -class Hexalith.Memories.Server.Tests.Actors.EmbeddingRateLimiterActorTests -class Hexalith.Memories.Server.Tests.Actors.RateLimiterLogicTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.IndexSyntacticActivityTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.IndexSemanticActivityTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.IndexSemanticChunksActivityTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.IndexNaturalLanguageSemanticActivityTests -class Hexalith.Memories.Server.Tests.Infrastructure.IndexSchemaDefinitionsTests -class Hexalith.Memories.Server.Tests.Search.SemanticSearchServiceTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.CleanupActivityTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.VerifyConsistencyActivityTests -class Hexalith.Memories.Server.Tests.Activities.Indexing.EnumerateMemoryUnitIdsActivityTests -class Hexalith.Memories.Server.Tests.Ingestion.ReIngestionCoordinatorTests -class Hexalith.Memories.Server.Tests.Ingestion.FailedUnitsRegistryTests -class Hexalith.Memories.Server.Tests.Infrastructure.TenantIndexReadinessVerifierTests -class Hexalith.Memories.Server.Tests.Architecture.IndexingHotPathGuardTests -class Hexalith.Memories.Server.Tests.EventStoreIntegration.RedisSearchIndexMaintenanceAdapterTests -class Hexalith.Memories.Server.Tests.Activities.Tenants.ProvisionRediSearchActivityTests -class Hexalith.Memories.Server.Tests.Activities.Tenants.ProvisionRedisVectorActivityTests -class Hexalith.Memories.Server.Tests.Architecture.IngestionWorkflowDeterminismGuardTests -class Hexalith.Memories.Server.Tests.Workflows.TenantProvisioningWorkflowTests -class Hexalith.Memories.Server.Tests.Workflows.IngestionWorkflowTests` -- observed 2026-08-02: total `337`, errors `0`, failed `0`, skipped `0`, not run `0`.
- `git diff --check` -- expected: no whitespace errors.
- `( set +e; for new_doc in _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md _bmad-output/implementation-artifacts/spec-keep-epic-23-ingestion-invariants-on-epic-24-and-epic-25-review-checklists.md; do new_doc_check_output="$(git diff --no-index --check -- /dev/null "$new_doc" 2>&1)"; new_doc_check_status=$?; test "$new_doc_check_status" = 1 || exit 1; test -z "$(printf '%s\n' "$new_doc_check_output" | sed '/^warning: in the working copy/d')" || exit 1; done )` -- expected: exit `0`; each underlying diff exits `1` only because the file is new, and removing the informational line-ending warning leaves no whitespace-error diagnostic.
- `git status --short` and `git diff --name-status` -- expected: no source, test, sprint-status, dependency, or submodule changes.

## Suggested Review Order

**Intent and canonical gate**

- Start with the approved problem statement and bounded documentation-only correction.
  [sprint-change-proposal-2026-08-02.md:11](../planning-artifacts/sprint-change-proposal-2026-08-02.md#L11)

- Review Epic 24's canonical six-invariant obligation and evidence rules.
  [epics.md:4526](../planning-artifacts/epics.md#L4526)

- Confirm Epic 25 carries the byte-identical canonical gate.
  [epics.md:4603](../planning-artifacts/epics.md#L4603)

**Reviewer-facing checklists**

- Confirm Epic 24 exposes six independently reviewable, linked statuses.
  [epic-24-context.md:52](epic-24-context.md#L52)

- Confirm Epic 25 exposes the same six review obligations.
  [epic-25-context.md:62](epic-25-context.md#L62)

**Corrective evidence**

- Inspect Epic 24's preserved partial history and current-tree evidence matrix.
  [epic-24-retro-2026-07-06.md:118](epic-24-retro-2026-07-06.md#L118)

- Inspect Epic 25's acknowledged omission and matching corrective evidence.
  [epic-25-retro-2026-07-12.md:126](epic-25-retro-2026-07-12.md#L126)

**Workflow proof**

- Review the two adversarial loop corrections and final evidence-boundary patches.
  [spec-keep-epic-23-ingestion-invariants-on-epic-24-and-epic-25-review-checklists.md:55](spec-keep-epic-23-ingestion-invariants-on-epic-24-and-epic-25-review-checklists.md#L55)

- Re-run the serialized tests and fail-closed structural checks.
  [spec-keep-epic-23-ingestion-invariants-on-epic-24-and-epic-25-review-checklists.md:61](spec-keep-epic-23-ingestion-invariants-on-epic-24-and-epic-25-review-checklists.md#L61)
