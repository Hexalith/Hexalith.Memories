---
baseline_commit: 462d37c14f7599d3d66f4fc8a38d9d8fb719f0e4
---

# Story 19.3: Release Preflight and Baseline Evidence Residual Sweep

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a release maintainer,
I want release-preflight and baseline-evidence carry-forwards reviewed as one release-quality backlog decision,
so that low-value hardening stays trigger-bound and high-value release risks get implementation stories.

## Acceptance Criteria

1. Given `12.4-RV20` requests optional strict literal per-SHA replay evidence, when release evidence needs are reviewed, then the team either creates a strict replay evidence story or records why ancestry-based proof remains sufficient until a release post-mortem or quality story reopens it.

2. Given `15.1-RV1` through `15.1-RV16` are carried forward from release-preflight review, when the sweep runs, then each entry is grouped into implement-now, accept-until-trigger, or future release-hardening story buckets.

3. Given release tooling changes can affect package publication, when any implement-now item is selected, then focused validation covers the changed script, workflow, and inventory-test behavior.

## Tasks / Subtasks

- [x] Task 1 — Establish the decision baseline before editing (read-only) (AC: 1, 2, 3)
  - [x] Read the 17 structured entries this story owns in `_bmad-output/implementation-artifacts/deferred-work.md`: `12.4-RV20` (currently ~line 166, inside the `## Story 15.5 Triage Rollup (2026-05-15)` section) and `15.1-RV1` … `15.1-RV16` (currently ~lines 497-622, inside `## Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)`). Confirm all 17 are `Status: carried-forward`. (Verify line numbers — the file shifts between runs; match on the `ID:` token, not the line.)
  - [x] Read the two rollup sections that route these IDs here: `## Deferred Register Backlog Home Rollup (2026-06-30)` (line ~52: "`12.4-RV20` and `15.1-RV1` through `15.1-RV16` target Story 19.3 unless the story explicitly accepts or reassigns them") and the `## Story 19.1 Classification Sweep (2026-06-30)` table (line ~84-85; both routed as "scheduled story → Story 19.3"; its anti-over-promotion note states "Stories 19.2/19.3/19.4 make the final implement/accept/defer call for the IDs they own"). Read the `## Story 19.2 Downstream Contract Artifact Decisions (2026-06-30)` section (line ~134) as the immediate-predecessor worked example of "make the final call + add a dated rollup".
  - [x] Read `_bmad-output/planning-artifacts/epics.md` Story 19.3 (lines ~3780-3798) and `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-30.md` Story 19.3 section (lines ~183-201), the Backlog Home Rollup (lines ~255-258), and the risk note (line ~95: "the main risk is over-promoting low-value accepted risks into mandatory implementation").
  - [x] Confirm the AC3 evidence surfaces exist but are NOT to be modified by the recommended path: `tools/release-preflight.ps1`, `tools/test-release.ps1`, `.github/workflows/release.yml`, `tests/tooling/release_preflight/release_preflight_test.py`, `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`, `docs/dev/release-runbook.md`, `_bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md`. These are the `Target artifact` homes the 17 entries point at; a real implement-now selection would touch one of the first five, which AC3 then gates with focused validation.
  - [x] Build the disposition decision in working notes before editing any file. Do NOT bulk-rewrite historical prose or touch any deferred-work entry other than the 17 owned IDs.

- [x] Task 2 — Make and record the `12.4-RV20` decision in place (AC: 1)
  - [x] Apply the **recommended disposition: accept-until-trigger** (`Status: carried-forward` → `accepted`). Edit the `12.4-RV20` block in place (see Dev Notes → Recommended field block for `12.4-RV20`), keeping all required fields valid. Update the human-readable summary-bullet label from `- carried-forward.` to `- accepted.` and the `  - Status:` line to `accepted`; keep `ID`, `Source story`, and `Target artifact` byte-for-byte; keep the existing `Re-open trigger` (release post-mortem traces a regression to a silently-fixed test at an anchor SHA, OR a release-quality story explicitly requests strict literal replay).
  - [x] The `Rationale:` MUST record the AC1 decision: ancestry-based HEAD-inheritance proof remains sufficient for current close-out evidence, the team declines to create the proposed "Strict Release Baseline Replay Evidence" story now, and the entry re-opens only via the named trigger. Name an **owner** (release maintainer) in `Rationale` prose — the schema has no `Owner:` field (see Parser guardrail).

- [x] Task 3 — Bucket and record the 16 `15.1-RV*` decisions in place (AC: 2)
  - [x] Apply the **recommended disposition: accept-until-trigger for all 16** (`Status: carried-forward` → `accepted`), with **zero implement-now** selections (see Dev Notes → Recommended bucketing and Why no implement-now). For each `15.1-RVn` block use the mechanical recipe in Dev Notes → Recommended edit recipe (15.1-RV*): flip the summary-bullet label to `- accepted.`, flip `  - Status:` to `accepted`, and append one sentence to the existing `  - Rationale:` line naming the bucket (accept-until-trigger), the natural future-sweep home, and an owner. Leave `ID`, `Source story`, `Target artifact`, and `Re-open trigger` byte-for-byte unchanged.
  - [x] Apply the AC2 anti-over-promotion rule: do NOT promote any of these low-value, no-current-trigger hardening items into a mandatory implement-now item. If the dev judges a specific entry to be a genuine high-value release risk that must ship now, that single entry may be selected implement-now — but then Task 5 / AC3 applies to it (focused validation of the changed script + workflow + inventory test), and it is no longer a Markdown-only decision sweep.
  - [x] Verify every edited block still parses: exactly one `ID`, one `Status` (now `accepted`), one `Source story`, one `Target artifact`, one `Re-open trigger`, and exactly one `Rationale:` (no `Evidence:` line — `Evidence:` is for `resolved` only).

- [x] Task 4 — Add the Story 19.3 decision rollup section (AC: 1, 2)
  - [x] Add a new `## Story 19.3 Release Preflight and Baseline Evidence Decisions (2026-06-30)` section to `deferred-work.md`, placed **immediately after the `## Story 19.2 Downstream Contract Artifact Decisions (2026-06-30)` section (ends ~line 151) and before `## Story 15.5 Triage Rollup (2026-05-15)` (currently ~line 153)**. Do not delete, reorder, or rewrite any existing section — including Story 19.1's classification table and Story 19.2's decisions section. Use the recommended text in Dev Notes → Recommended rollup section text.
  - [x] In the new section, record: the final decision for `12.4-RV20` (accept-until-trigger / ancestry-proof sufficient, AC1); the bucket assignment for all 16 `15.1-RV*` (recommended: accept-until-trigger, implement-now = none, AC2); and the natural future-release-hardening-story groupings (release-preflight script robustness; release test-helper hardening; docs/governance hygiene) that name where each entry lands **if** its trigger fires — without scheduling any story now.
  - [x] State explicitly that this sweep makes the final implement/accept/defer call per the Backlog Home Rollup's "unless the story explicitly accepts or reassigns them" clause, references completed Epics 12/15 (the entries' source stories) without reopening them, and — because the recommended path selects no implement-now item and changes no release tooling — **AC3's focused-validation obligation is not triggered** (record this conditional explicitly).

- [x] Task 5 — Validate the governance lane (AC: 1, 2, 3)
  - [x] Run the deferred-work parser regression guard if the SDK + time budget permit (load-bearing): build, then `DiffEngine_Disabled=true dotnet exec <built Hexalith.Memories.Cli.Tests dll>` filtered to `CiTestInventoryTests` (see Dev Notes → Running tests). Flipping the 17 blocks `carried-forward` → `accepted` keeps them valid (`accepted` still requires `Rationale:`, which all retain). If the SDK lane is unavailable, record the limitation and at minimum run `git diff --check -- _bmad-output/implementation-artifacts/deferred-work.md` (CR-as-trailing-whitespace warnings on CRLF lines are the expected, documented repo behavior — see deferred entry `15.5-RV1` / Epic 18 Action Item 1).
  - [x] **AC3 conditional:** if Task 3 selected ANY implement-now item that edited a release surface, run focused validation that covers the changed script (`tools/release-preflight.ps1`), the workflow (`.github/workflows/release.yml`), and the inventory-test behavior (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` and/or `tests/tooling/release_preflight/release_preflight_test.py`). If the recommended path was followed (no implement-now, Markdown only), record that AC3's condition did not fire and only the parser guard ran.
  - [x] Confirm there are NO production source changes (`src/**`), NO release-tooling changes (`tools/**`, `.github/workflows/**`), NO test changes (`tests/**`), NO `docs/**` changes, and NO submodule-pointer changes in the diff (this is a planning/governance decision sweep). Confirm `deferred-work.md` and this story file use **CRLF**; do not touch `sprint-status.yaml` line endings beyond the create-story status-row update (it is **LF**).
  - [x] Record the 17 final dispositions, the implement-now = none decision (and AC3 no-fire), and any validation limits in this story's Dev Agent Record and Change Log.

## Dev Notes

This is a **planning/governance decision sweep**, not a code change — the same lane as Stories 19.1 and 19.2 (its immediate predecessors). It edits only `deferred-work.md` and this story file (`sprint-status.yaml` is updated by the create-story / dev-story status transitions only). It does NOT change release tooling (`tools/release-preflight.ps1`, `tools/test-release.ps1`, `.github/workflows/release.yml`), does NOT touch `src/**`, `tests/**`, `docs/**`, and does NOT change submodule pointers. The deliverable is 17 recorded disposition decisions plus a dated rollup. [Source: planning-artifacts/sprint-change-proposal-2026-06-30.md lines 95, 298 ("implement Story 19.1 first, then use its classification to decide whether Stories 19.2 through 19.4 need implementation, accepted-debt records, or smaller split stories"); epics.md Story 19.3 ACs]

### What this story decides (the 17 owned entries)

This story makes the final implement/accept/defer call for the 17 IDs that Story 19.1 routed here (`12.4-RV20` plus `15.1-RV1` … `15.1-RV16`). All 17 are currently `carried-forward`; the story converts them from "carried-forward, awaiting Story 19.3's call" into a bounded decision. The story goal frames the test for each: **low-value hardening stays trigger-bound; only a high-value release risk earns an implementation story.** None of the 17 has a current failure, a pulling consumer, or a release that is blocked today — each was deferred precisely because its fix is a release-owner policy decision with no live trigger. The recommended call is therefore **accept-until-trigger for all 17, with zero implement-now**, which directly honors the Epic 19 anti-over-promotion guardrail.

#### `12.4-RV20` (AC1)

| Field | Current | Recommended |
|---|---|---|
| Status | `carried-forward` | `accepted` (accept-until-trigger) |
| Decision | "Proposed follow-up story owns the optional drill" | Ancestry-based HEAD-inheritance proof is sufficient for current close-out evidence; decline to schedule the strict replay evidence story now |
| Re-open trigger | (unchanged) | release post-mortem traces a regression to a test that existed at a named anchor SHA but was silently fixed before HEAD, OR a release-quality story explicitly requests strict literal replay over ancestry-based proof |

AC1 offers exactly two valid outcomes: **create a strict replay evidence story**, or **record why ancestry-based proof remains sufficient until a release post-mortem or quality story reopens it.** The recommended call is the second (accept-until-trigger), because strict literal per-SHA replay is a release-quality proof *candidate*, not a runtime defect, and the existing HEAD-inheritance rationale already covers existing close-out evidence. Flipping to `accepted` (per the schema: "the risk remains but is intentionally accepted with a written rationale") ends the routing limbo, exactly as Story 19.2 did for its two entries. [Source: epics.md Story 19.3 AC1; deferred-work.md `12.4-RV20` block lines 160-171]

#### `15.1-RV1` … `15.1-RV16` (AC2) — recommended bucketing

AC2 requires each entry grouped into **implement-now**, **accept-until-trigger**, or **future release-hardening story**. Recommended assignment: **implement-now = none; all 16 = accept-until-trigger**, with each entry's natural future-sweep home recorded in the rollup so it is pre-routed if its trigger ever fires. The three natural future-story groupings (informational; not scheduled now):

| Natural future home (on trigger only) | Entries | `Target artifact` family | Why accept-until-trigger now |
|---|---|---|---|
| Release-preflight script robustness sweep | `15.1-RV1` (network retry/backoff), `15.1-RV2` (semantic-release version-regex i18n contract), `15.1-RV3` (catch loses inner exception/stack), `15.1-RV13` (`git show-ref` exit-128 diagnostic), `15.1-RV15` (`Resolve-Path` UX on bad `-RepositoryPath`) | `tools/release-preflight.ps1` | No release has failed on any of these; each fix is a release-owner policy choice (retry window, output contract, error verbosity), not a clear patch. |
| Release test-helper hardening sweep | `15.1-RV4` (strict `ShouldBe` brittleness), `15.1-RV5` (Windows tempdir cleanup), `15.1-RV6` (`Path \| None` Py3.10+), `15.1-RV7` (non-UTF-8 codepage), `15.1-RV8` (`git init` default branch), `15.1-RV9` (`pwsh` skip-guard), `15.1-RV14` (peeled-only-ref fixture), `15.1-RV16` (4-space-indent job-scalar parser) | `tests/tooling/release_preflight/release_preflight_test.py`; `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` | Tests pass in current CI/dev today; RV4/RV16 explicitly say the fix "should be done as a sweep across the file, not in isolation." |
| Docs / governance hygiene | `15.1-RV10` (stale "step 7" cross-refs after runbook renumber), `15.1-RV11` (`S11-FC` trigger names script by path), `15.1-RV12` (`12.1-RV3` accepted-until 2026-08-13 has no automated reminder) | `docs/dev/release-runbook.md`; `_bmad-output/implementation-artifacts/deferred-work.md` | Doc-hygiene / governance-tooling gaps with no current consumer harm; `15.1-RV12`'s date (2026-08-13) is the closest thing to a real future trigger but the reminder infra does not exist yet. |

[Source: deferred-work.md `15.1-RV1` … `15.1-RV16` blocks (lines 496-622); each entry's own `Rationale` already states "no current trigger / low impact / CI works today"]

### Why no implement-now (anti-over-promotion)

Epic 19's single biggest risk, called out in the change proposal, is "over-promoting low-value accepted risks into mandatory implementation" — Story 19.1 was required to "explicitly separate 'schedule now' from 'accept until trigger'." Every one of the 17 entries was deferred with an explicit "no evidence the path manifests yet / recoverable / out of scope" rationale. Selecting any as implement-now would (a) contradict its own deferral rationale, (b) trip AC3's focused-validation obligation, and (c) convert a Markdown-only governance sweep into a release-tooling change with real regression surface. The recommended path keeps all 17 trigger-bound. If the dev genuinely believes one entry is a high-value release risk (the strongest candidates are `15.1-RV1` network-retry and `15.1-RV2` version-regex, both of which could in principle abort a release lane), selecting *that one* implement-now is AC-valid — but then it must satisfy AC3. [Source: sprint-change-proposal-2026-06-30.md lines 95, 187; epics.md Story 19.3 goal line]

### Recommended field block for `12.4-RV20` (apply in place)

Replace the existing `12.4-RV20` block (summary bullet + field block, currently ~lines 160-171) with:

```
- **12.4-RV20 - accepted.** Strict literal per-SHA replay evidence is a
  release-quality proof candidate, not a runtime defect. Ancestry-based HEAD-inheritance
  proof remains acceptable for existing close-out evidence; Story 19.3 declines to
  schedule a strict replay evidence story now.

  - ID: 12.4-RV20
  - Status: accepted
  - Source story: 15-5-deferred-register-triage-sweep
  - Target artifact: _bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md; tools/test-release.ps1; tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
  - Re-open trigger: A release post-mortem traces a regression to a test that existed at one of the named anchor SHAs but was silently fixed before HEAD, or a release-quality story explicitly requests strict literal replay evidence over ancestry-based proof.
  - Rationale: Story 19.3 (2026-06-30) reviewed the release-evidence need (AC1) and accepts ancestry-based HEAD-inheritance proof as sufficient for current close-out evidence, declining to create the proposed "Strict Release Baseline Replay Evidence" story until the re-open trigger fires. This governance sweep does not run historical checkout/build/test lanes or mutate release tooling. Owner: release maintainer.
```

Keep the two-space indentation on the field sub-bullets exactly so the parser's anchored regex still matches. Only the summary-bullet label, the `Status:` line, the summary prose, and the `Rationale:` line change; `ID`, `Source story`, `Target artifact`, and `Re-open trigger` stay byte-for-byte.

### Recommended edit recipe (`15.1-RV*`)

For each of the 16 `15.1-RVn` blocks, apply the same mechanical three-line change (do NOT rewrite the whole block):

1. Summary bullet: `- **15.1-RVn - carried-forward.** …` → `- **15.1-RVn - accepted.** …` (label only; keep the description text).
2. `  - Status: carried-forward` → `  - Status: accepted`.
3. Append one sentence to the existing `  - Rationale:` line: ` Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: <release-preflight script robustness | release test-helper hardening | docs/governance hygiene> sweep. Owner: release maintainer.`

Leave `ID`, `Source story`, `Target artifact`, and `Re-open trigger` byte-for-byte unchanged on every block. Worked example for `15.1-RV1`:

```
- **15.1-RV1 - accepted.** Transient network failure in `Test-RemoteTagCollision` aborts the release lane.
  - ID: 15.1-RV1
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: tools/release-preflight.ps1
  - Re-open trigger: A release attempt fails because `git ls-remote` returns a transient network/DNS error and the preflight has no retry/backoff.
  - Rationale: The preflight currently has no retry. A DNS hiccup turns a recoverable error into a hard abort. Deferred because the right policy (number of retries, backoff window, idempotency boundary) is a release-owner decision rather than a clear patch. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no release has failed on it; the retry policy is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.
```

(Alternative, also AC-valid: keep the script-robustness and test-helper clusters `carried-forward` and refresh each to name a concrete future hardening story + owner — that exercises the literal "future release-hardening story" bucket per entry. The recommended accept-until-trigger path is cleaner, lower-churn, and ends the limbo, matching Story 19.2; the rollup still records the future-story homes either way. Do NOT mix: pick one disposition per entry and keep every block parser-valid.)

### Recommended rollup section text (Task 4)

Insert after the Story 19.2 section, before the Story 15.5 section:

```
## Story 19.3 Release Preflight and Baseline Evidence Decisions (2026-06-30)

Story 19.3 makes the final implement/accept/defer call for the 17 release-quality
carry-forwards that Story 19.1 routed here: `12.4-RV20` (strict baseline replay evidence)
and `15.1-RV1` … `15.1-RV16` (the 15.1 release-preflight code-review residuals). All 17 are
bucketed accept-until-trigger with **no implement-now selection**, because none has a current
failure, a pulling consumer, or a blocked release, and each fix is a release-owner policy
decision rather than a clear patch — promoting any of them now would violate the Epic 19
anti-over-promotion guardrail. The natural future-release-hardening-story homes below are
recorded so a triggered entry is pre-routed; no such story is scheduled now.

| Entry(ies) | Decision | Natural future home on trigger |
|---|---|---|
| `12.4-RV20` | accepted (ancestry-based proof sufficient; AC1) | a release-quality "Strict Release Baseline Replay Evidence" story, only if a post-mortem or quality story reopens it |
| `15.1-RV1`, `15.1-RV2`, `15.1-RV3`, `15.1-RV13`, `15.1-RV15` | accepted (accept-until-trigger) | release-preflight script robustness sweep (`tools/release-preflight.ps1`) |
| `15.1-RV4`, `15.1-RV5`, `15.1-RV6`, `15.1-RV7`, `15.1-RV8`, `15.1-RV9`, `15.1-RV14`, `15.1-RV16` | accepted (accept-until-trigger) | release test-helper hardening sweep (preflight pytest + `CiTestInventoryTests`) |
| `15.1-RV10`, `15.1-RV11`, `15.1-RV12` | accepted (accept-until-trigger) | docs/governance hygiene (`docs/dev/release-runbook.md`; deferred-register tooling) |

No implement-now item was selected, so AC3's focused-validation obligation (changed script +
workflow + inventory test) does not fire; only the `CiTestInventoryTests` deferred-work parser
guard was run. Completed Epics 12 and 15 (the entries' source stories) are referenced, never
reopened. [Source: epics.md Story 19.3 ACs; sprint-change-proposal-2026-06-30.md Story 19.3 +
risk note line 95; deferred-work.md Backlog Home Rollup + Story 19.1 classification table]
```

### Parser guardrail (load-bearing regression risk)

`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` parses `deferred-work.md` structured entries and **fails loudly** (`ShouldAssertException`) on malformed blocks. Every edited active entry MUST satisfy:

- `AllowedDeferredStatuses = ["open", "resolved", "accepted", "carried-forward"]` — closed, lowercase. `accepted` is valid; synonyms (`done`, `closed`, `not-needed`, `declined`) fail. [CiTestInventoryTests.cs:25]
- Required fields present: `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`. A missing required field fails loudly. [CiTestInventoryTests.cs:365-374]
- `accepted` requires `Rationale:` (NOT `Evidence:`). Every flipped block keeps its `Rationale:` line and must not gain an `Evidence:` line. [CiTestInventoryTests.cs:376-431]
- The `ID:` value is matched as a verbatim whitespace-free token (`StructuredIdShape`: alphanumeric, dot, dash). `12.4-RV20` and `15.1-RV1` … `15.1-RV16` are unchanged — keep them exactly, as they are referenced from the rollups, from `epics.md`, and from the Story 19.1 classification table. [deferred-work.md#Schema lines 12-14]
- The schema has NO `Owner:` field — an `Owner:` line is silently ignored by the parser. Record the owner in prose inside the `Rationale:` line (as the recommended blocks do), not as a structured field. [CiTestInventoryTests.cs StructuredFieldRegex]
- `12.4-RV20`'s `Target artifact` references `tools/test-release.ps1`, but the entry is NOT itself a release-lane baseline (`S11-F*` family) and carries no `Test:` field today — do NOT add a `Test:` field when flipping its status. The 16 `15.1-RV*` are likewise not release-lane baselines. [deferred-work.md#Schema "Optional fields"]

Field block format (anchored at the start of each indented sub-bullet):

```
- ID: <verbatim id>
  - Status: <open|resolved|accepted|carried-forward>
  - Source story: <story/retro/review key>
  - Target artifact: <repo-relative path or planning artifact>
  - Re-open trigger: <one sentence>
  - Rationale: <required for accepted/carried-forward>   # Evidence: only for resolved
```

[Source: deferred-work.md#Schema-for-Active-Entries lines 3-50; 19-1 / 19-2 dev notes Parser guardrail; CiTestInventoryTests.cs]

### Pattern precedents (reuse, do not reinvent)

- **Story 19.2** (immediate predecessor, `_bmad-output/implementation-artifacts/19-2-downstream-contract-artifact-generation-decisions.md`, status `review`) is the closest analog: it took two routed `carried-forward` IDs, flipped them to `accepted` with refreshed `Rationale` + owner, added a dated `## Story 19.2 …` rollup section after the Story 19.1 section, and ran the `CiTestInventoryTests` parser guard (48/48). Mirror its rollup-heading + in-place-flip + "preserve historical prose, no submodule churn, CRLF/LF hygiene" discipline exactly. [Source: 19-2-…md Tasks 2-5, Dev Agent Record]
- **Story 19.1** (`19-1-…md`, status `review`) is the upstream that routed all 17 of these IDs to Story 19.3 and established the active-entry inventory, the anti-over-promotion separation, and the parser-guard run recipe. [Source: 19-1-…md Classification Sweep + Dev Agent Record]
- **Story 15.5** (`15-5-deferred-register-triage-sweep.md`) is the original bounded-triage precedent that added a dated rollup section and applied `accepted` dispositions (e.g. `12.4-RV10`, `12.4-RV11`); it is also the `Source story` for `12.4-RV20`. Its `accepted` blocks are worked examples of the exact shape this story applies. [Source: deferred-work.md lines 153-217]
- **Story 14.5** introduced the structured-field schema and the structured-field parser. [Source: 14-5-deferred-register-governance-and-sprint-status-hygiene.md]

### Scope boundaries — what this story must NOT do

- Do NOT implement any of the 17 hardening items (no retry/backoff in the preflight, no regex/JSON contract change, no test-helper edits, no runbook renumber sweep, no accepted-until reminder infra). This story records *decisions*, not implementations. The recommended path selects zero implement-now. [Source: epics.md Story 19.3 ACs; sprint-change-proposal-2026-06-30.md line 95]
- Do NOT edit any deferred-work entry other than the 17 owned IDs; do NOT rewrite Story 19.1's classification table, Story 19.2's decisions section, the Story 15.5 rollup, or any historical prose/heading. [Source: Epic 19 Preflight "Do not bulk-rewrite historical deferred prose"; sprint-change-proposal-2026-06-30.md line 137]
- Do NOT modify release tooling or its tests unless an implement-now item is deliberately selected — `tools/release-preflight.ps1`, `tools/test-release.ps1`, `.github/workflows/release.yml`, `tests/tooling/release_preflight/release_preflight_test.py`, `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`, `docs/dev/release-runbook.md`, and `_bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md` are `Target artifact` references, not edit targets, for the recommended decision-only path. (If an implement-now item IS selected, AC3 then requires focused validation of the changed script + workflow + inventory test.) [Source: epics.md Story 19.3 AC3]
- Do NOT change `sprint-status.yaml` `action_items:`. This story's entries are not named in any Epic 17/18 retrospective action item (those were closed by Story 19.1). Only update an action item when its acceptance condition newly becomes met (none do here). [Source: sprint-status.yaml action_items; 19-1-…md Action Item 4 closure]
- Do NOT reopen `epic-12` or `epic-15` (both `done`); only reference the completed source stories (`12-4-baseline-failures-sweep`, `15-1-release-edge-case-preflight-hardening`, `15-5-deferred-register-triage-sweep`). [Source: epics.md Story 19.1 AC3 pattern; sprint-change-proposal-2026-06-30.md]

### Latest tech context (informs the re-open triggers only — no implementation here)

These notes exist so the re-open triggers point at real behavior if a future story schedules implementation. They are NOT a signal to implement now, and no package is added.

- `12.4-RV20`'s "ancestry-based proof" = the release-baseline-failure sweep proves a named test still exists at HEAD by git-ancestry inheritance from the anchor SHAs, rather than by literally checking out each SHA and re-running the test (the "strict literal per-SHA replay"). The current evidence in `12-4-baseline-failures-sweep.md` relies on the ancestry argument; the strict replay drill is the heavier, optional proof this story declines to schedule.
- `15.1-RV2`'s "more stable contract" would be `semantic-release --dry-run --debug` JSON (or a plugin hook) instead of regex-matching the English "The next release version is X.Y.Z" line — relevant only if semantic-release rewords or i18n's that output. No change is made now.
- `15.1-RV6`/`15.1-RV9` portability notes (`Path | None` needs Python 3.10+; `pwsh` skip-guard) matter only if the project lowers its Python floor or runs the preflight tests on a host without PowerShell 7; current CI is Python 3.11+ with pwsh present.
- No version research is required for this decision story because no package is added and no code is written. [Source: project-context.md — central package management; this story adds no `PackageReference`]

### Testing standards

- Tests are xUnit v3 + Shouldly. The only test relevant to this governance lane is `CiTestInventoryTests` (the `deferred-work.md` structured-entry parser). No new tests are required for a decision sweep, but the parser must stay green after the 17 `Status` flips. [Source: project-context.md Testing Rules; 19-1 / 19-2 Testing standards]
- Running tests in this sandbox: plain `dotnet test` fails (VSTest `SocketException (13)`). Build, then `DiffEngine_Disabled=true dotnet exec` the built `Hexalith.Memories.Cli.Tests` dll, filtering to `CiTestInventoryTests` (e.g. `-class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`). This is the Epic 17/18/19.1/19.2 carry-forward workaround. [Source: project memory "Running .NET tests in this sandbox"; 19-2-…md Debug Log (CiTestInventoryTests 48/48)]
- AC3 validation (only if an implement-now item is selected): a `release-preflight.ps1` change is covered by `tests/tooling/release_preflight/release_preflight_test.py`; a `.github/workflows/release.yml` change is covered by the `CiTestInventoryTests` release-workflow assertions (`ReleaseWorkflow_*`). Run both for any selected implement-now item.

### Line endings (avoid whole-file churn — Epic 18 Action Item 1)

- This story file and `deferred-work.md` are **CRLF** (`.editorconfig` `end_of_line = crlf`). Keep them CRLF; an editor that rewrites them to LF produces a whole-file diff and a review finding. Edit only the 17 field blocks + the new rollup section; leave the rest of `deferred-work.md` byte-for-byte. (Note: `deferred-work.md` currently has a small number of historical lone-LF lines — preserve them; do not normalize the whole file. See 19-1-…md Debug Log "53 pre-existing lone-LF lines".)
- `sprint-status.yaml` is currently **LF** in the working tree — the create-story / dev-story status-row transition must preserve LF; do not flip it to CRLF and do not touch it beyond the `19-3-…` status row.
- [Source: .editorconfig lines 3-9; `file` on the targets; project memory "CRLF line endings required"; 19-1 / 19-2 Line endings note]

### Project Structure Notes

- Edited files live under `_bmad-output/implementation-artifacts/`: `deferred-work.md` (17 in-place field-block flips + one new rollup section) and this story file. `sprint-status.yaml` is updated only by the status transitions (create-story: `19-3-…` `backlog` → `ready-for-dev`; dev-story: `ready-for-dev` → `in-progress` → `review`).
- No conflicts with the unified project structure: this story edits Markdown governance artifacts only (under the recommended decision-only path); it introduces no new code paths, namespaces, packages, routes, or tests.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic-19 / Story-19.3 (lines 3780-3798) — story statement and the three ACs]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-30.md — Story 19.3 section (lines 183-201); Backlog Home Rollup (lines 255-258); anti-over-promotion risk (line 95); developer-agent sequencing (line 298)]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md#12.4-RV20 (lines 160-171); #15.1-RV1…15.1-RV16 (lines 496-622); #Deferred-Register-Backlog-Home-Rollup-2026-06-30 (lines 52-64); #Story-19.1-Classification-Sweep-2026-06-30 (lines 66-99); #Story-19.2-Downstream-Contract-Artifact-Decisions-2026-06-30 (lines 134-151); #Schema-for-Active-Entries (lines 3-50)]
- [Source: _bmad-output/implementation-artifacts/19-2-downstream-contract-artifact-generation-decisions.md — immediate-predecessor pattern (in-place flip to accepted + dated rollup + parser guard), Parser guardrail, CRLF/LF hygiene]
- [Source: _bmad-output/implementation-artifacts/19-1-deferred-register-active-entry-classification-sweep.md — upstream routing of all 17 IDs to Story 19.3, anti-over-promotion separation, parser-guard run recipe]
- [Source: _bmad-output/implementation-artifacts/15-5-deferred-register-triage-sweep.md — bounded-triage precedent and `Source story` for `12.4-RV20`; worked `accepted` blocks]
- [Source: tools/release-preflight.ps1; tools/test-release.ps1; .github/workflows/release.yml; tests/tooling/release_preflight/release_preflight_test.py; docs/dev/release-runbook.md; _bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md — `Target artifact` homes (referenced, not modified on the recommended path)]
- [Source: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs — deferred-work parser contract + release-workflow assertions for the AC3 conditional]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status (epic-19 rows) and #STATUS-DEFINITIONS]
- [Source: _bmad-output/project-context.md — repo-wide rules (CRLF, central package management, submodule discipline, no production-code change here)]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-30: Loaded BMAD dev-story workflow customization (no prepend/append activation steps), project config, project context, story file, and sprint status; preserved existing `baseline_commit`.
- 2026-06-30: Verified Task 1 evidence: all 17 owned structured entries (`12.4-RV20`, `15.1-RV1`...`15.1-RV16`) were `Status: carried-forward`; Story 19.1/Backlog Home Rollup route them to Story 19.3; Story 19.2 provides the immediate predecessor decision pattern; target release/tooling/doc/test artifacts exist but are not edit targets on the recommended path.
- 2026-06-30: Applied the recommended decision path in `deferred-work.md`: all 17 entries accepted until trigger, zero implement-now selections, no release tooling/test/docs/source changes.
- 2026-06-30: Verified each edited block has exactly one `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, and `Rationale`, with `Status: accepted` and no `Evidence:` line.
- 2026-06-30: `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj -v:n` failed during restore/build startup with no MSBuild errors or warnings emitted.
- 2026-06-30: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` passed: 48 total, 0 failed.
- 2026-06-30: Broader existing CLI test assembly run reached 415 tests; 2 failed due sandbox socket permission in `QuickstartPrerequisiteTests` TCP listener setup, unrelated to this Markdown governance sweep.
- 2026-06-30: `git diff --check -- _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/19-3-release-preflight-and-baseline-evidence-residual-sweep.md _bmad-output/implementation-artifacts/sprint-status.yaml` reports CRLF-as-trailing-whitespace warnings on Markdown additions; this is the documented expected behavior for CRLF deferred-work/story edits.
- 2026-06-30: Story-scoped diff review found no Story 19.3 production source, release-tooling, test, docs, or `.github/**` edits; unrelated working-tree changes outside the Story 19.3 File List were left untouched.
- 2026-06-30: Senior Developer Review verified AC1/AC2 dispositions, parser-relevant fields, line endings, and sprint tracking; corrected the scope-note wording above and moved Story 19.3 to done.

### Completion Notes List

- Final disposition recorded for `12.4-RV20`: `accepted` / accept-until-trigger; ancestry-based HEAD-inheritance proof remains sufficient for current close-out evidence, and no strict replay evidence story is scheduled unless the re-open trigger fires.
- Final disposition recorded for all 16 `15.1-RV*` entries: `accepted` / accept-until-trigger, grouped into natural future homes for release-preflight script robustness, release test-helper hardening, and docs/governance hygiene.
- Added Story 19.3 rollup documenting implement-now = none, AC3 no-fire, completed Epics 12/15 referenced but not reopened, and the future-release-hardening homes if triggers fire.
- Validation passed for the load-bearing deferred-work parser guard (`CiTestInventoryTests` 48/48). Full rebuild was blocked by an undiagnosed MSBuild startup failure; broader existing CLI assembly run was blocked only by sandbox TCP listener permission in two quickstart prerequisite tests.
- Senior Developer Review found one bookkeeping issue: the scope note could be read as claiming a fully clean working tree despite unrelated dirty files outside Story 19.3. The note now distinguishes Story 19.3 scope from unrelated working-tree changes.

### File List

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/19-3-release-preflight-and-baseline-evidence-residual-sweep.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-30.

Outcome: Approve after automatic fix. No CRITICAL issues remain.

### Findings Fixed

- [MEDIUM] Scope-note wording was too broad for the current repository state. The Dev Agent Record claimed no tracked `src/**`, `tests/**`, `docs/**`, `tools/**`, or `.github/**` changes, but the working tree contains unrelated dirty files outside Story 19.3's File List. Fixed by narrowing the statement to the Story 19.3 scoped diff and explicitly noting unrelated working-tree changes were left untouched.

### Validation Notes

- AC1 is satisfied: `12.4-RV20` is accepted with an ancestry-proof rationale, no strict replay story is scheduled, and the re-open trigger remains explicit.
- AC2 is satisfied: `15.1-RV1` through `15.1-RV16` are accepted until trigger and grouped into the release-preflight script robustness, release test-helper hardening, and docs/governance hygiene future homes.
- AC3 did not fire because implement-now selections remain zero and no release tooling/workflow/test surface was changed by Story 19.3.
- File List matches the Story 19.3 review surface: `deferred-work.md`, this story file, and `sprint-status.yaml`.
- Review validation reran `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`: 48 total, 0 failed.
- `git diff --check` reports CRLF-as-trailing-whitespace on the newly added CRLF Markdown review lines; this is the documented expected behavior for these artifacts.
- Line endings remain as required: this story file and `deferred-work.md` use CRLF; `sprint-status.yaml` uses LF.

## Change Log

| Date | Phase | Summary |
|---|---|---|
| 2026-06-30 | create-story | Story drafted: release-quality decision sweep for `12.4-RV20` + `15.1-RV1`…`15.1-RV16` (17 entries). Recommended disposition = accept-until-trigger for all 17 (flip `carried-forward` → `accepted`), zero implement-now per the Epic 19 anti-over-promotion guardrail; `12.4-RV20` accepts ancestry-based proof as sufficient (AC1); 16 `15.1-RV*` bucketed accept-until-trigger with natural future-sweep homes recorded (AC2); AC3 focused-validation does not fire because no implement-now item is selected. No production/release-tooling/test/doc/submodule scope. Status → ready-for-dev. |
| 2026-06-30 | dev-story | Accepted `12.4-RV20` and `15.1-RV1`...`15.1-RV16` until trigger, added the Story 19.3 decision rollup, selected zero implement-now items so AC3 focused release-tooling validation did not fire, ran the deferred-work parser guard successfully, and moved status to review. |
| 2026-06-30 | review | Senior Developer Review fixed the scope-note wording, verified AC1/AC2/AC3 conditional behavior, confirmed no critical issues remain, and moved status to done. |
