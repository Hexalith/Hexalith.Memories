---
baseline_commit: 462d37c14f7599d3d66f4fc8a38d9d8fb719f0e4
---

# Story 19.4: Provider Registry and Migration Residual Sweep

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a system operator,
I want provider-registry and migration-marker residual risks reviewed against current code,
so that embedding-provider expansion and live migration do not inherit stale assumptions.

## Acceptance Criteria

1. Given `15.2-RV1` through `15.2-RV9` are marked `open`, when the sweep runs, then each item is either resolved, accepted with rationale, or assigned to a concrete provider-registry follow-up story.

2. Given migration-marker deferred entries from Story 15.3 include concurrency, stale-marker, TTL, and operator-documentation risks, when the sweep runs, then the team identifies which risks remain trigger-bound and which need a migration-hardening story before the next provider migration investment.

3. Given provider/model casing and registry dispatch appear in both provider and migration paths, when follow-up work is scheduled, then tests cover both write-time validation and read/runtime comparison paths where practical.

## Tasks / Subtasks

- [x] Task 1 - Establish the decision baseline before editing (AC: 1, 2, 3)
  - [x] Read the nine structured entries this story owns in `_bmad-output/implementation-artifacts/deferred-work.md`: `15.2-RV1` ... `15.2-RV9` (currently in `## Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)`). Confirm each is still `Status: open`. Match on the `ID:` token, not line numbers.
  - [x] Read the migration-marker legacy prose entries this story owns: `15.3-RV6`, `15.3-RV8`, `15.3-RV10`, `15.3-RV13`, `15.3-RV15`, `15.3-RV16`, `15.3-RV18`, `15.3-RV22`, `15.3-RV24`, `15.3-RV25`, `15.3-RV26`, and `15.3-RV27` under `## Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)`. These are legacy prose, not active structured entries; do not bulk-migrate them unless the selected disposition requires a new structured follow-up.
  - [x] Read the rollup sections that route this work here: `## Deferred Register Backlog Home Rollup (2026-06-30)` and `## Story 19.1 Classification Sweep (2026-06-30)`. Note the anti-over-promotion rule: Stories 19.2/19.3/19.4 make the final implement/accept/defer call for their owned IDs.
  - [x] Read `_bmad-output/planning-artifacts/epics.md` Story 19.4 and `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-30.md` Story 19.4 section plus the risk note that the main Epic 19 risk is over-promoting low-value accepted risks into mandatory implementation.
  - [x] Read the current code surfaces before deciding: `EmbeddingProviderDefaults.cs`, `EmbeddingClient.cs`, `TenantConfigurationActor.cs`, `GenerateEmbeddingActivity.cs`, `EmbeddingVectorMigrationService.cs`, `EmbeddingMigrationMarkerReader.cs`, `RedisEmbeddingMigrationStore.cs`, focused provider tests, focused migration marker tests, and `docs/operations/embedding-providers.md`.
  - [x] Build the disposition decision in working notes before editing any file. Do not rewrite unrelated deferred-work entries or historical prose.

- [x] Task 2 - Decide and record all nine `15.2-RV*` provider-registry entries (AC: 1, 3)
  - [x] Apply the recommended disposition unless current code review proves a stronger action is needed: accept-until-trigger for all nine `15.2-RV*` entries, with zero implement-now selections. Each accepted block must keep `ID`, `Source story`, `Target artifact`, and `Re-open trigger` stable unless a concrete follow-up story is explicitly selected.
  - [x] For `15.2-RV1`, record that contract serialization remains unchanged unless a future casing/canonicalization story changes contract semantics. The current contract tests preserve JSON shape and value round-trip; validation lives in server/provider paths, not in the contract serializer.
  - [x] For `15.2-RV2`, record that same-provider/different-model actor reindex coverage is trigger-bound until a supported provider has a second model in the closed registry, or `GetBreakingChangeFields(...)` regresses.
  - [x] For `15.2-RV3`, record that the unregistered literals in tests are fixture-hygiene debt unless those paths begin validating tenant configs or a registry-wide test-fixture sweep is scheduled.
  - [x] For `15.2-RV4`, `15.2-RV5`, and `15.2-RV6`, record these as the provider-dispatch and persisted-casing cluster. If any third-provider or second-model work is scheduled, create or assign a concrete provider-registry follow-up that covers `EmbeddingClient` dispatch, persisted `provider:model` parsing, `GenerateEmbeddingActivity` write form, and migration target default selection together.
  - [x] For `15.2-RV7`, record this as accepted until an operator UX issue justifies trimming/whitespace-specific diagnostics in `EmbeddingProviderDefaults.Validate(...)`.
  - [x] For `15.2-RV8`, record this as accepted until operator visibility for already-persisted invalid configs is explicitly needed; do not add read-time validation that could break compatibility without an approved remediation path.
  - [x] For `15.2-RV9`, record this as accepted until the order-sensitive metric test flakes in CI or a test-isolation sweep is scheduled.
  - [x] If any `15.2-RV*` item is selected implement-now instead of accepted, update the story scope and run the focused tests named under Testing Standards. AC3 then requires tests for both write-time validation and read/runtime comparison paths where practical.

- [x] Task 3 - Decide and record the Story 15.3 migration-marker residuals (AC: 2, 3)
  - [x] Add a Story 19.4 rollup table that identifies which `15.3-RV*` items remain trigger-bound and which become mandatory before the next provider migration investment. Do not edit the legacy prose bullets unless creating new structured follow-up entries.
  - [x] Recommended trigger-bound decisions: `15.3-RV6` (optional pre-provider marker guard when keyed Redis is missing), `15.3-RV8` (`WaitAsync(ct)` does not cancel Redis command), `15.3-RV10` (completed active-marker hash keeps stale target metadata), `15.3-RV13` (case-insensitive marker comparison versus case-sensitive Redis metadata), `15.3-RV22` (weak historical rationale), `15.3-RV24` (status-transition process flag), and `15.3-RV26` (future culture-sensitive integer parsing regression).
  - [x] Recommended "before next provider migration investment" hardening cluster: `15.3-RV15`, `15.3-RV16`, and `15.3-RV27` (active marker target-consistency, completion target matching, and resume-target drift). If a new provider migration story is scheduled, these should be bundled as a concrete migration-marker target-consistency story.
  - [x] Recommended operator-recovery cluster: `15.3-RV18` and `15.3-RV25` (orphaned active marker without TTL/manual-clearance guidance and sharper downtime/retry wording). These remain accepted until an operator escalation or the next migration-investment story, but must be reconsidered before a production migration claim.
  - [x] Confirm current docs already state the committed durable-marker behavior, active-marker retry/failure semantics, and no global ingestion pause. Do not change docs unless the selected disposition schedules operator-recovery work now.

- [x] Task 4 - Add the Story 19.4 decision rollup section (AC: 1, 2, 3)
  - [x] Add `## Story 19.4 Provider Registry and Migration Residual Decisions (2026-06-30)` to `_bmad-output/implementation-artifacts/deferred-work.md`, immediately after the Story 19.3 rollup section and before `## Story 15.5 Triage Rollup (2026-05-15)`.
  - [x] Record the final decision for every `15.2-RV*` entry and every owned `15.3-RV*` legacy prose item. State explicitly whether implement-now is zero or name the exact follow-up story selected.
  - [x] State that this story makes the final call under the Backlog Home Rollup's "unless the story explicitly accepts or reassigns them" clause, references completed Stories 15.2 and 15.3 without reopening Epic 15, and preserves historical prose except for any explicitly scheduled structured follow-up entries.

- [x] Task 5 - Validate the governance and optional implementation lane (AC: 1, 2, 3)
  - [x] If the recommended decision-only path is followed, run the deferred-work parser guard: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`. If the SDK lane is unavailable, record the blocker and at minimum run `git diff --check -- _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/19-4-provider-registry-and-migration-residual-sweep.md`.
  - [x] Manually verify every edited structured block has exactly one `ID`, one `Status` from `{open, resolved, accepted, carried-forward}`, one `Source story`, one `Target artifact`, one `Re-open trigger`, and `Rationale:` for accepted/carried-forward or `Evidence:` for resolved. Do not add a standalone `Owner:` field.
  - [x] If provider-registry code is changed, run focused server tests for `EmbeddingProviderDefaultsTests`, `EmbeddingClientTests`, `TenantConfigurationActorTests`, and contract serialization tests if casing/canonicalization changes.
  - [x] If migration-marker code is changed, run focused server tests for `EmbeddingVectorMigrationServiceTests`, `GenerateEmbeddingActivityTests`, `IndexSemanticActivityTests`, and `IndexNaturalLanguageSemanticActivityTests`.
  - [x] Confirm there are no unintended `src/**`, `tests/**`, `docs/**`, `tools/**`, `.github/**`, or submodule-pointer changes outside any explicitly selected implement-now scope. Preserve CRLF for this story file and `deferred-work.md`; preserve LF for `sprint-status.yaml`.

## Dev Notes

This story is a **planning/governance decision sweep by default**, like Stories 19.2 and 19.3. It should edit only `_bmad-output/implementation-artifacts/deferred-work.md` and this story file unless the dev deliberately selects an implement-now provider or migration hardening item. `sprint-status.yaml` is updated only by BMad status transitions. Do not reopen Epic 15; reference completed Stories 15.2 and 15.3 as sources. [Source: sprint-change-proposal-2026-06-30.md lines 95, 298; epics.md Story 19.4 ACs]

### Current Code Signals

`EmbeddingProviderDefaults` already owns a closed local registry for Google and Ollama. It has provider-scoped allowed models/dimensions, max rate-limit ceilings, default config factories, `MaxSupportedDimensions = 16_384`, case-insensitive provider/model lookup, and preserved caller casing. That means the original high-risk cross-provider validation defects are closed, but the registry is not a runtime dispatch abstraction. [Source: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs; deferred-work.md Story 15.2 rollup]

`EmbeddingClient` still dispatches through `IsGoogle(...)` and `IsOllama(...)`, and `ParseEmbeddingProviderIdentifier(...)` accepts only those two providers, lowercases the provider, and preserves the model after the first colon. This is acceptable while the registry contains only two providers, but it is the exact future third-provider trigger behind `15.2-RV4` and related casing items. [Source: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs]

`GenerateEmbeddingActivity` returns persisted identifiers as `$"{config.Provider}:{config.Model}"`, so mixed-case tenant config can create a write form that differs from the parser's lowercased provider form. The current migration marker guard compares provider/model with `OrdinalIgnoreCase`, so this is accepted compatibility today, not a current defect. [Source: GenerateEmbeddingActivity.cs; EmbeddingMigrationMarkerReader.cs]

`EmbeddingVectorMigrationService.TryBuildTargetConfig(...)` still chooses Google defaults if `TargetProvider` is Google and Ollama defaults otherwise, then relies on `EmbeddingProviderDefaults.Validate(...)` to reject unsupported providers. That is a binary defaulting path, not a registry-driven provider factory. It is safe for two providers but must be revisited with any third provider or migration target expansion. [Source: EmbeddingVectorMigrationService.cs]

`RedisEmbeddingMigrationStore` writes both per-target and active-marker hashes atomically. Completion stamps `status=completed` plus `completedAt` on both keys but does not target-match the active marker before completion. Resume checks the per-target marker exists but does not verify the active marker still points to the same target. These are low-frequency operator-coordination risks today and become mandatory before the next provider migration investment. [Source: RedisEmbeddingMigrationStore.cs; deferred-work.md `15.3-RV15`, `15.3-RV16`, `15.3-RV27`]

### Recommended Provider-Registry Dispositions

Recommended path: **accept-until-trigger for all nine `15.2-RV*` entries, no implement-now**. The risks are real but dormant because the registry currently has one model per provider and only two runtime providers. Scheduling them now would turn a governance sweep into broad provider-architecture work without a pulling provider or operator incident.

| ID | Recommended decision | Natural future home on trigger |
|---|---|---|
| `15.2-RV1` | accepted | contract casing/canonicalization story, only if the contract boundary changes |
| `15.2-RV2` | accepted | provider-registry model-expansion tests when a second model lands under one provider |
| `15.2-RV3` | accepted | registry-wide test-fixture hygiene sweep |
| `15.2-RV4` | accepted | provider-runtime dispatch abstraction story for a third provider |
| `15.2-RV5` | accepted | provider identifier canonicalization story covering write/read/migration equality |
| `15.2-RV6` | accepted | migration target factory/registry story for a third provider |
| `15.2-RV7` | accepted | provider validation UX cleanup if whitespace diagnostics become operator-visible |
| `15.2-RV8` | accepted | operator visibility/remediation story for persisted invalid configs |
| `15.2-RV9` | accepted | test-isolation sweep if the metric test flakes |

If a dev selects any implement-now item, the strongest coherent cluster is `15.2-RV4` + `15.2-RV5` + `15.2-RV6`: runtime dispatch, persisted identifier casing, and migration target selection must be solved together. Do not fix one in isolation unless a current failing test proves the narrower change.

### Recommended Migration-Marker Dispositions

Recommended path: add rollup decisions for the legacy `15.3-RV*` prose without migrating every historical bullet into structured field blocks. The legacy prose remains valid historical context; Story 19.4 only needs to make the forward-looking decision visible.

| Legacy item(s) | Recommended decision | Required before next provider migration investment? |
|---|---|---|
| `15.3-RV6`, `15.3-RV8`, `15.3-RV10`, `15.3-RV13`, `15.3-RV22`, `15.3-RV24`, `15.3-RV26` | trigger-bound accepted risks | No, unless their own re-open trigger fires |
| `15.3-RV15`, `15.3-RV16`, `15.3-RV27` | migration-marker target-consistency cluster | Yes, before scheduling a new provider migration investment |
| `15.3-RV18`, `15.3-RV25` | operator-recovery and operator-copy cluster | Reassess before production migration claims or operator-facing migration investment |

This satisfies AC2 without pretending every migration-marker review note has equal urgency. The target-consistency cluster is the only clearly code-shaped migration-hardening story: detect an existing active marker for another target, verify completion target matches the active marker, and make resume refuse drifted active marker state.

### Recommended Edit Recipe

For each `15.2-RV*` structured block selected as accepted:

1. Summary bullet: change `- **15.2-RVn - open.**` to `- **15.2-RVn - accepted.**`.
2. Field line: change `  - Status: open` to `  - Status: accepted`.
3. Keep `ID`, `Source story`, `Target artifact`, and `Re-open trigger` unless assigning a concrete follow-up story.
4. Replace or append `Rationale:` so it names Story 19.4, the accepted-until-trigger reason, the natural future home, and an owner in prose. Do not add `Owner:` as a separate field.

For the `15.3-RV*` legacy prose items, do not mechanically add structured blocks. Add the Story 19.4 rollup table and only create a new structured `carried-forward` entry if an actual follow-up story is scheduled.

### Parser Guardrail

`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` parses structured entries in `deferred-work.md`. Every edited structured block must satisfy:

- `Status` is one of `open`, `resolved`, `accepted`, or `carried-forward`.
- Required fields: `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`.
- `accepted` and `carried-forward` require `Rationale:`; `resolved` requires `Evidence:`.
- `ID:` must stay a single verbatim token. Keep `15.2-RV1` ... `15.2-RV9` exact.
- There is no `Owner:` field. Put owner text inside `Rationale:`.

[Source: deferred-work.md#Schema-for-Active-Entries; 19-1/19-2/19-3 parser guardrail notes; CiTestInventoryTests.cs]

### Scope Boundaries

- Do NOT implement a provider plugin system, remote model discovery, operator-editable registry, or generic provider factory unless a concrete third-provider story is actually scheduled.
- Do NOT change contract JSON shape or canonicalize provider/model casing casually. Provider/model values can be contract-visible and migration-visible.
- Do NOT add read-time tenant config rejection for already-persisted invalid configs without an operator remediation path.
- Do NOT clear, expire, or overwrite migration active markers casually. The current durable marker is the safety gate for live migration.
- Do NOT edit `docs/operations/embedding-providers.md` unless operator-recovery or downtime wording is intentionally selected for implementation.
- Do NOT touch submodules or submodule pointers.

### Testing Standards

Decision-only path: run `CiTestInventoryTests` to prove the structured deferred-work edits parse. No provider or migration tests are needed if no code changes.

Provider-registry implementation path: run focused tests for `EmbeddingProviderDefaultsTests`, `EmbeddingClientTests`, `TenantConfigurationActorTests`, and `TenantEmbeddingConfigSerializationTests` if contract/casing behavior changes.

Migration-marker implementation path: run focused tests for `EmbeddingVectorMigrationServiceTests`, `GenerateEmbeddingActivityTests`, `IndexSemanticActivityTests`, and `IndexNaturalLanguageSemanticActivityTests`. If `RedisEmbeddingMigrationStore` marker target matching changes, add tests proving existing active-marker conflict, completion target mismatch, and resume active-marker drift fail closed.

Plain `dotnet test` may hit the known VSTest `SocketException (13)` sandbox limitation. Use the built xUnit v3 executable with `DiffEngine_Disabled=true` for the parser guard, as Stories 19.1 through 19.3 did.

### Line Endings

This story file and `deferred-work.md` should remain CRLF. `sprint-status.yaml` is LF. Preserve those line endings and avoid whole-file churn.

### Project Structure Notes

Recommended edited files are `_bmad-output/implementation-artifacts/deferred-work.md` and this story file only. `sprint-status.yaml` changes only through create-story/dev-story status transitions. There is no expected change to `src/**`, `tests/**`, `docs/**`, `tools/**`, `.github/**`, package files, or submodules unless an implement-now item is deliberately selected and recorded.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-19.4 (lines 3800-3818) - story statement and ACs]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-30.md - Epic 19 scope, anti-over-promotion risk, and Story 19.4 routing]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md#Deferred-Register-Backlog-Home-Rollup-2026-06-30 and #Story-19.1-Classification-Sweep-2026-06-30]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md#Deferred-from-code-review-of-15-2-provider-model-dimension-registry-2026-05-14 - `15.2-RV1` ... `15.2-RV9`]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md#Deferred-from-code-review-of-15-3-live-migration-coordination-policy-2026-05-14 - `15.3-RV*` legacy prose]
- [Source: _bmad-output/implementation-artifacts/15-2-provider-model-dimension-registry.md - registry implementation precedent and out-of-scope review findings]
- [Source: _bmad-output/implementation-artifacts/15-3-live-migration-coordination-policy.md - durable marker policy, file scope, and migration safety invariants]
- [Source: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs - closed provider/model/dimension registry]
- [Source: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs - runtime dispatch and persisted identifier parser]
- [Source: src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs - persisted provider:model write form and pre-provider marker guard]
- [Source: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs - target config selection and live migration flow]
- [Source: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs and RedisEmbeddingMigrationStore.cs - active marker semantics]
- [Source: docs/operations/embedding-providers.md - operator migration behavior matrix]
- [Source: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs - deferred-work parser contract]
- [Source: _bmad-output/project-context.md - repo rules for C#, tests, CRLF, package management, and submodules]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context)

### Debug Log References

- Parser guard (decision-only path): `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` -> **Total: 48, Failed: 0** (the test reads the live repo `deferred-work.md` via `Path.Combine(repoRoot, ...)`, so it validated the edited file).
- CR-at-EOL-aware whitespace check: `git -c core.whitespace=cr-at-eol diff --check -- deferred-work.md 19-4-*.md` -> no real whitespace errors. (Plain `git diff --check` flags every CRLF line as "trailing whitespace" — the known CR-at-EOL artifact for this repo's CRLF files, see `15.5-RV1`; it is not a real defect.)
- Current-code signal verification (read-only) of `EmbeddingProviderDefaults.cs`, `EmbeddingClient.cs`, `GenerateEmbeddingActivity.cs`, `EmbeddingVectorMigrationService.cs`, `EmbeddingMigrationMarkerReader.cs`, `RedisEmbeddingMigrationStore.cs`: all six signals CONFIRMED against the recommended dispositions.

### Completion Notes List

Decision-only governance sweep (no production code, tests, or docs changed). Path matches the recommended disposition; **zero implement-now selections**.

- **AC1 — `15.2-RV1` … `15.2-RV9`:** all nine flipped `open` -> `accepted` (accept-until-trigger) in their structured blocks, each `Rationale` now names Story 19.4, the accept-until-trigger reason, the natural future home, and an owner in prose; `ID`/`Source story`/`Target artifact`/`Re-open trigger` left stable; no standalone `Owner:` field added. Dispositions were validated against current code: the closed registry still holds exactly two providers (Google, Ollama) with one model each, `EmbeddingClient` still binary-dispatches `IsGoogle`/`IsOllama` and lowercases the parsed provider, and `GenerateEmbeddingActivity` still writes the raw `$"{config.Provider}:{config.Model}"` form — so every risk is real-but-dormant, justifying accept over implement-now.
- **AC2 — Story 15.3 migration-marker residuals:** added a Story 19.4 rollup table classifying the twelve owned legacy-prose items into (a) trigger-bound accepted (`15.3-RV6/RV8/RV10/RV13/RV22/RV24/RV26`), (b) the migration-marker target-consistency cluster mandatory before the next provider-migration investment (`15.3-RV15/RV16/RV27`), and (c) the operator-recovery cluster to reassess before any production migration claim (`15.3-RV18/RV25`). Legacy prose bullets were left unchanged (no bulk-migration to structured blocks, since no follow-up story was scheduled). Code confirmed the cluster split: `RedisEmbeddingMigrationStore` completion does not target-match the active marker, resume does not verify the active-marker target, and the active marker has no TTL.
- **AC3 — test-path note:** because no implement-now provider/migration item was selected, AC3's write-time-plus-read/runtime focused-test obligation does not fire; only the `CiTestInventoryTests` deferred-work parser guard was run, as for Stories 19.1–19.3.
- **Task 4 rollup section:** `## Story 19.4 Provider Registry and Migration Residual Decisions (2026-06-30)` added immediately after the Story 19.3 rollup and before `## Story 15.5 Triage Rollup (2026-05-15)`, recording the final call for every owned ID, stating the final-call authority under the Backlog Home Rollup clause, referencing completed Stories 15.2/15.3 without reopening Epic 15, and preserving historical prose.
- **Conditional subtasks resolved N/A (no action required):** Task 2 "implement-now" branch (no item selected implement-now) and Task 5 focused provider/migration test runs (no provider or migration code changed). Docs untouched — `docs/operations/embedding-providers.md` already states durable-marker behavior, active-marker retry/failure semantics, and no global ingestion pause.
- **Line endings / scope:** `deferred-work.md` is uniformly CRLF after editing (the editor normalized ~47 stray LF lines left by the 19.1–19.3 rollup edits to CRLF, consistent with the repo CRLF convention and this story's "preserve CRLF" directive); the content diff verified via `--ignore-cr-at-eol` is exactly the nine `15.2-RV*` dispositions plus the new Story 19.4 section. This story made **no** `src/**`, `tests/**`, `docs/**`, `tools/**`, `.github/**`, or submodule-pointer changes. (Evidence-packet `src`/`docs`/`tests` files showing as modified in the working tree are from a concurrent story-automator session on Story 2.7 — not touched by this run and excluded from the File List below.)

### File List

- `_bmad-output/implementation-artifacts/deferred-work.md` (modified — nine `15.2-RV*` blocks `open` -> `accepted`; new Story 19.4 decision rollup section)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — `19-4-...` status `ready-for-dev` -> `in-progress` -> `review` -> `done`)
- `_bmad-output/implementation-artifacts/19-4-provider-registry-and-migration-residual-sweep.md` (modified — task checkboxes, Dev Agent Record, Senior Developer Review, Status -> done, Change Log)

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-30.

Outcome: Approved after automatic fixes. No critical issues remain.

### Findings Fixed

- [MEDIUM] Story status was still `review` after the decision sweep and parser validation were complete. Fixed by setting this story's `Status` to `done`.
- [MEDIUM] Sprint tracking still showed `19-4-provider-registry-and-migration-residual-sweep: review`, so the workflow status was not synced after review. Fixed in `sprint-status.yaml`.
- [LOW] The story lacked the required `Senior Developer Review (AI)` record and review changelog entry. Fixed here.

### Validation Notes

- AC1 is satisfied: `15.2-RV1` through `15.2-RV9` are all `accepted` with stable `ID`, `Source story`, `Target artifact`, and `Re-open trigger` fields, plus Story 19.4 rationales and no standalone `Owner:` field.
- AC2 is satisfied: the Story 19.4 rollup classifies the owned Story 15.3 migration-marker residuals into trigger-bound accepted risks, the target-consistency cluster required before the next provider-migration investment, and the operator-recovery cluster to reassess before production migration claims.
- AC3 did not fire because implement-now selections remain zero and no provider-registry or migration-marker code changed.
- Current-code evidence was re-checked against `EmbeddingProviderDefaults`, `EmbeddingClient`, `GenerateEmbeddingActivity`, `EmbeddingVectorMigrationService`, `EmbeddingMigrationMarkerReader`, `RedisEmbeddingMigrationStore`, and `docs/operations/embedding-providers.md`.
- File List matches the Story 19.4 review surface: `deferred-work.md`, this story file, and `sprint-status.yaml`; unrelated dirty working-tree files outside Story 19.4 scope were left untouched.
- MCP resource discovery returned no configured resources; no web documentation lookup was required because this story adds no package, API, or source implementation and relies on current repository evidence.
- Review validation reran `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`: 48 total, 0 failed.
- CR-at-EOL-aware whitespace validation passed for `deferred-work.md`, this story file, and `sprint-status.yaml`; line endings remain as required (`deferred-work.md` and this story file CRLF, `sprint-status.yaml` LF).

## Change Log

| Date | Phase | Summary |
|---|---|---|
| 2026-06-30 | create-story | Story drafted for provider-registry and migration-marker residual decisions. Recommended path is a decision-only sweep: accept-until-trigger for `15.2-RV1` ... `15.2-RV9`, roll up legacy `15.3-RV*` migration-marker items, and schedule code only if a third-provider or next-provider-migration investment trigger exists. Status -> ready-for-dev. |
| 2026-06-30 | dev-story | Decision-only sweep executed; current code re-verified (six signals CONFIRMED). Flipped all nine `15.2-RV*` entries `open` -> `accepted` with Story 19.4 rationales; added the Story 19.4 decision rollup section with the `15.3-RV*` trigger-bound vs migration-hardening classification. Zero implement-now; no production code/test/doc changes. Parser guard `CiTestInventoryTests` 48/48 pass. Status -> review. |
| 2026-06-30 | review | Senior review found and fixed review bookkeeping, revalidated AC1/AC2/AC3 conditional behavior, reran the deferred-work parser guard, and moved status to done. |
