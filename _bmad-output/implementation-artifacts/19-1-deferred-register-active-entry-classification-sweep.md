---
baseline_commit: 462d37c14f7599d3d66f4fc8a38d9d8fb719f0e4
---

# Story 19.1: Deferred Register Active-Entry Classification Sweep

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want every active `open` or `carried-forward` deferred-work entry to have a current disposition,
so that completed epics do not hide unscheduled operational or consumer-risk work.

## Acceptance Criteria

1. Given `deferred-work.md` contains structured entries with `Status: open` or `Status: carried-forward`, when the sweep runs, then every active structured entry is classified as one of: scheduled story, accepted debt with rationale, carried-forward with explicit trigger and owner, or resolved with evidence.

2. Given Epic 18 retrospective Action Item 4 names parked carry-forwards, when the sweep runs, then `MEM-2-ASPIRATE`, `MEM-3-OPENAPI`, the real-Redis race evidence, the Dapr-sidecar pub/sub smoke evidence, and the Story 18.4 token-anchoring edge each receive a story id or accepted-debt entry with a re-open trigger.

3. Given active entries from completed Epics 15 and 18 may still be valid, when the sweep updates planning artifacts, then it references the completed source story but does not reopen the completed epic or alter completed story history.

4. Given `sprint-status.yaml` has retrospective action items, when the sweep completes, then related action items are updated only when their acceptance condition is actually met.

## Tasks / Subtasks

- [x] Task 1 — Establish the sweep baseline before editing (read-only) (AC: 1, 2, 3, 4)
  - [x] Read `_bmad-output/implementation-artifacts/deferred-work.md` in full and confirm the active structured-entry set below (30 entries: 9 `open`, 21 `carried-forward`). Treat the Story 14.5 structured schema (the field block at the top of the file) as the source of truth for "active". Ignore legacy prose entries and entries with `Status: resolved`/`accepted`.
  - [x] Read `_bmad-output/implementation-artifacts/epic-18-retro-2026-06-25.md` Action Item 4 (the five parked carry-forwards) and Action Item 2 (stale architecture anchors); read `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-30.md` (Story 19.1 section + the Deferred Register Backlog Home Rollup); read the `action_items:` list in `sprint-status.yaml`.
  - [x] Confirm Stories 19.2/19.3/19.4 are still `backlog` (not `in-progress`/`review`). If one is active, do not re-disposition its owned IDs beyond cross-referencing. Confirm `epic-15` and `epic-18` are `done` — they must not be reopened.
  - [x] Build the disposition inventory in working notes before editing any file. Do NOT bulk-rewrite historical prose; only classify active structured entries.

- [x] Task 2 — Classify every active structured entry (AC: 1, 3)
  - [x] Add a new `## Story 19.1 Classification Sweep (2026-06-30)` rollup section to `deferred-work.md` (place it directly under the existing `## Deferred Register Backlog Home Rollup (2026-06-30)` section, above the Story 15.5 rollup). Do not delete or reorder existing sections.
  - [x] For the 28 entries already routed by the Backlog Home Rollup, record disposition = **scheduled story** and cross-reference the owning story. Do NOT rewrite or duplicate their existing structured blocks:
    - `15.2-RV1`…`15.2-RV9` (all `open`) → Story 19.4.
    - `12.4-RV20` and `15.1-RV1`…`15.1-RV16` (all `carried-forward`) → Story 19.3.
    - `MEM-2-ASPIRATE` and `MEM-3-OPENAPI` (`carried-forward`) → Story 19.2 (see Task 3 for the AC2 confirmation).
  - [x] For the 2 residual entries NOT owned by 19.2/19.3/19.4, make the actual disposition decision and update each entry's structured block in place (keeping all six fields valid):
    - `1.1-RR3` (line ~1249; Aspire `CS0618` obsolete `WithReference` suppression). Recommended: keep `carried-forward` but satisfy AC1's "explicit trigger **and owner**" — name the owner (AppHost/release maintainer) **in prose within the `Rationale:` line**, since the schema has no `Owner:` field. The existing trigger (Aspire 14.x bump turns the warning into an error, or a non-obsolete CommunityToolkit.Aspire.Hosting.Dapr binding API ships) stays.
    - `MEM-1` (line ~1416; clean-clone `.slnx` build + project-symbol-resolution guard, source `parties-consumer-integration-intake-2026-05-27`, originally "scheduled as Story 18.1"). Story 18.1 is now `done`, so it can no longer be "scheduled". Verify Story 18.1 completion evidence: if 18.1 delivered the compile-resolution guard + name-stability contract, mark MEM-1 `resolved` with that evidence (reference `18-1-...md`; do NOT reopen Epic 18); otherwise re-classify as `carried-forward` with a refreshed trigger + owner for the residual non-reflectable `PackageId` half. Pick exactly one and record it.
  - [x] Apply the AC1 anti-over-promotion rule: separate "schedule now" from "accept until trigger". Do not promote a low-value accepted risk into mandatory implementation.

- [x] Task 3 — Give the five Epic-18 parked carry-forwards explicit homes (AC: 2)
  - [x] `MEM-2-ASPIRATE` and `MEM-3-OPENAPI`: confirm each already carries a structured `carried-forward` block (lines ~1428 and ~1440) and is routed to **Story 19.2** (a real backlog story id). No new entry required — record in the Task 2 rollup that the story-id home is Story 19.2, satisfying Action Item 4 for these two.
  - [x] Create THREE new structured entries (full six-field schema) for the items that exist only in retro prose today. Use `Source story: 19-1-deferred-register-active-entry-classification-sweep`; reference the originating stories/retro WITHOUT reopening Epic 18:
    - **real-Redis two-thread race test (Story 18.4)** — recommended `ID: 18.4-REDIS-RACE`, `Status: accepted` (infra-lane-deferred). `Target artifact:` the 18.4 concurrency integration test / Aspire-Testcontainers lane. `Re-open trigger:` before any production claim about concurrent ingest, run the real two-thread Redis race wherever a Docker/Aspire lane is available. `Rationale:` substitute-proven (deterministic winner/loser test) and unit-proven today; this sandbox cannot run the Docker/Aspire lane.
    - **Dapr-sidecar pub/sub smoke (Story 18.8)** — recommended `ID: 18.8-DAPR-SMOKE`, `Status: accepted`. `Re-open trigger:` before any production claim about cross-module event delivery, run the Dapr-sidecar pub/sub smoke wherever a Docker/Aspire lane is available. `Rationale:` in-process HTTP E2E tests over `/events/ingest` stand in today; sandbox cannot run the sidecar lane.
    - **Story 18.4 token-anchoring edge** — recommended `ID: 18.4-TOKEN-EDGE`, `Status: accepted` (or a future hardening story id if you prefer to schedule it). `Re-open trigger:` a token whose first use falls back to a pre-existing `sourceUri` unit relying on the 24h reservation key (not a permanent record) causes a real dedup/idempotency defect, or a hardening story is scheduled. `Rationale:` tokens augment and never replace the permanent source-URI dedup record; the edge is a known narrow case.
  - [x] Each new/edited block MUST pass the parser contract (see Dev Notes → Parser guardrail): exactly one `ID`, one `Status` from `{open, resolved, accepted, carried-forward}`, one `Source story`, one `Target artifact`, one `Re-open trigger`, and `Evidence:` (only for `resolved`) or `Rationale:` (for `accepted`/`carried-forward`).

- [x] Task 4 — Update sprint-status action items only when their condition is met (AC: 3, 4)
  - [x] Re-evaluate Epic 18 **Action Item 4** ("Give the parked carry-forwards real backlog homes …"). Its done-condition is: "Each item is a story id or an accepted-debt entry in `deferred-work.md` with a re-open trigger." If Tasks 2–3 gave all five items a story id (`MEM-2-ASPIRATE`/`MEM-3-OPENAPI` → Story 19.2) or an accepted-debt entry with a re-open trigger (the three test/edge items), set this action item to `done` with a concise dated note referencing Story 19.1; otherwise leave it `open`/`in-progress`. Verify each of the five before changing the status.
  - [x] Do NOT change any other action item whose acceptance condition is not actually met (e.g. Action Item 2 architecture-anchor reconciliation is a separate doc task; this sweep only classifies, it does not rewrite `architecture.md`, so leave Action Item 2 `open`).
  - [x] Preserve all sprint-status comments, the STATUS DEFINITIONS block, and the `development_status` rows. Do NOT alter completed Epic 15/18 rows or story history. Update `last_updated:` to the current date if you edit the file. Preserve the file's existing **LF** line endings.

- [x] Task 5 — Validate the governance lane (AC: 1, 2, 3, 4)
  - [x] Run the deferred-work parser coverage if the local SDK and time budget permit (this is the load-bearing regression guard): `DiffEngine_Disabled=true dotnet exec <built CiTestInventoryTests dll>` filtered to `CiTestInventoryTests` (see Dev Notes → Running tests). If the SDK lane is unavailable, record that limitation and at minimum run `git diff --check -- _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/19-1-deferred-register-active-entry-classification-sweep.md`.
  - [x] Manually verify every new/edited structured block contains exactly one `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, and the required `Evidence` or `Rationale` line, with `Status` drawn from the closed lowercase vocabulary.
  - [x] Confirm there are NO submodule pointer changes and NO production source changes in the diff (this is a planning/governance sweep). Confirm `deferred-work.md` edits use CRLF and the new story file uses CRLF.
  - [x] Record the final disposition list, the Action Item 4 decision, and any validation limits in this story's Dev Agent Record.

## Dev Notes

This is a **planning/governance sweep**, not a code change. It edits only `deferred-work.md`, `sprint-status.yaml`, and this story file. It does NOT run historical checkout/build/test lanes, does NOT mutate release tooling, does NOT touch production `src/**`, and does NOT change submodule pointers. [Source: planning-artifacts/sprint-change-proposal-2026-06-30.md (Story 19.1 is low-effort, classification-only); deferred-work.md#12.4-RV20 rationale]

### Active structured-entry inventory (the AC1 universe — 30 entries)

Confirmed by full read of `deferred-work.md` on 2026-06-30. "Active" = `Status: open` or `Status: carried-forward` in a Story-14.5 structured field block. Legacy prose bullets and `resolved`/`accepted` entries are out of the AC1 universe.

| Disposition action | Entries | Owner / target |
|---|---|---|
| Affirm **scheduled story** (do not rewrite blocks) | `15.2-RV1`…`15.2-RV9` (open) | Story 19.4 |
| Affirm **scheduled story** | `12.4-RV20`, `15.1-RV1`…`15.1-RV16` (carried-forward) | Story 19.3 |
| Affirm **scheduled story** + AC2 confirm | `MEM-2-ASPIRATE`, `MEM-3-OPENAPI` (carried-forward) | Story 19.2 |
| **19.1 decides + records** (residuals) | `1.1-RR3`, `MEM-1` (carried-forward) | This story |
| **19.1 creates new structured entries** (AC2, retro-prose only today) | real-Redis race (18.4), Dapr-sidecar pub/sub smoke (18.8), 18.4 token-anchoring edge | This story |

Counts: `open` = 9 (all `15.2-RV*`); `carried-forward` = 21. The 28 already-routed entries (9 → 19.4, 17 → 19.3, 2 → 19.2) need only a classification record (scheduled story) — their existing blocks stay byte-for-byte intact. Only `1.1-RR3` and `MEM-1` require an actual new decision among the four AC1 dispositions. [Source: deferred-work.md#Deferred-Register-Backlog-Home-Rollup-2026-06-30 (lines 52-64); full-file structured-entry scan]

Caveat: the rollup also names "Story 15.3 migration-marker residuals" for Story 19.4, but the `15.3-RV*` items are legacy **prose** bullets (no field block) — they are NOT active structured entries and are out of this sweep's AC1 scope. Do not migrate them here; that is Story 19.4's call. [Source: deferred-work.md `15.3-RV*` prose region]

### The five Epic-18 parked carry-forwards (AC2 detail)

From Epic 18 retro Action Item 4 (owner Amelia, category Process). Done-condition: "Each item is a story id or an accepted-debt entry in `deferred-work.md` with a re-open trigger."
- `MEM-2-ASPIRATE` — aspirate/manifest emission deferred by Story 18.2. Already a structured `carried-forward` block; home = Story 19.2 (story id). [Source: epic-18-retro#Action-Item-4; deferred-work.md#MEM-2-ASPIRATE]
- `MEM-3-OPENAPI` — OpenAPI/Swagger generation deferred by Story 18.3. Already structured `carried-forward`; home = Story 19.2 (story id). [Source: deferred-work.md#MEM-3-OPENAPI]
- real-Redis two-thread race test (Story 18.4) — heaviest concurrency proof; runs only in an Aspire/Testcontainers lane the sandbox cannot execute; substitute-proven + unit-proven today. Needs a NEW accepted-debt entry. [Source: epic-18-retro lines 28, 103, 158]
- Dapr-sidecar pub/sub smoke (Story 18.8) — cross-module delivery smoke; same infra-lane deferral; in-process HTTP E2E tests stand in. Needs a NEW accepted-debt entry. [Source: epic-18-retro lines 28, 153, 158]
- Story 18.4 token-anchoring edge — "a token whose first use falls back to a pre-existing `sourceUri` unit relies on a 24h reservation key, not a permanent record"; flagged for a future hardening story. Tokens augment, never replace, the permanent dedup record. Needs a NEW accepted-debt entry (or a scheduled hardening story id). [Source: epic-18-retro lines 103, 129, 202]

### Scope boundaries — what this story must NOT do

- Do NOT reopen `epic-15` or `epic-18` (both `done`); only **reference** the completed source story. Do NOT alter completed story history. [Source: epics.md Story 19.1 AC3; sprint-change-proposal-2026-06-30.md lines 42-44, 58, 157]
- Do NOT bulk-rewrite historical deferred prose; classify only active structured entries that need planning signal. [Source: sprint-change-proposal-2026-06-30.md line 137 (Epic 19 Preflight)]
- Do NOT over-promote low-value accepted risks into mandatory implementation; explicitly separate "schedule now" from "accept until trigger." [Source: sprint-change-proposal-2026-06-30.md line 95]
- Do NOT update a sprint-status action item unless its acceptance condition is actually met. [Source: epics.md Story 19.1 AC4]
- Do NOT re-disposition IDs owned by Stories 19.2/19.3/19.4 beyond recording "scheduled story" — those stories make the real implement/accept/defer calls. The rollup routing is a default "unless the story explicitly accepts or reassigns them." [Source: sprint-change-proposal-2026-06-30.md lines 250-259]

### Parser guardrail (load-bearing regression risk)

`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` parses `deferred-work.md` structured entries and **fails loudly** (`ShouldAssertException`) on malformed blocks. Every new/edited active entry MUST satisfy:
- `AllowedDeferredStatuses = ["open", "resolved", "accepted", "carried-forward"]` — closed, lowercase. Synonyms (`done`, `closed`, `fixed`, `deferred-again`) fail. [CiTestInventoryTests.cs:25]
- Required fields present: `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`. A missing required field fails loudly. [CiTestInventoryTests.cs:365-374]
- `resolved` requires `Evidence:`; `accepted`/`carried-forward` requires `Rationale:`. Missing either fails loudly. [CiTestInventoryTests.cs:376-431]
- The `ID:` value is matched as a verbatim token; near-matches do not count. Keep IDs exactly as referenced elsewhere. It MUST be a single whitespace-free token of allowed characters only — alphanumeric, dot, dash (`StructuredIdShape`). A spaced description in `ID:` fails the test. [deferred-work.md#Schema lines 12-14; CiTestInventoryTests.cs:990]
- The schema has NO `Owner:` field — an `Owner:` line is silently ignored by the parser. Record any owner in prose inside the `Rationale:` line or the rollup section, not as a structured field. [CiTestInventoryTests.cs StructuredFieldRegex]
- Release-lane baseline entries (`S11-F*` family pointing at `tools/test-release.ps1`) additionally need a `Test:` field; none of this story's entries are release-lane baselines, so do not add `Test:`. [deferred-work.md#Schema "Optional fields"]

Field block format (anchored at the start of each indented sub-bullet), per the prior sweeps:
```
- **<id> - <status>.** <one-line human summary>

  - ID: <verbatim id>
  - Status: <open|resolved|accepted|carried-forward>
  - Source story: <story/retro/review key>
  - Target artifact: <repo-relative path or planning artifact>
  - Re-open trigger: <one sentence>
  - Evidence: <required iff resolved>   # OR
  - Rationale: <required iff accepted/carried-forward>
```
[Source: deferred-work.md#Schema-for-Active-Entries lines 3-50; Story 15.5 rollup blocks as worked examples]

### Pattern precedents (reuse, do not reinvent)

- **Story 14.5** introduced the schema and replaced the prose-heuristic parser with the structured-field parser. Same governance lane; same "do not bulk-migrate, only target set" discipline. [Source: 14-5-deferred-register-governance-and-sprint-status-hygiene.md]
- **Story 15.5** is the closest analog: a bounded triage sweep that added a dated rollup section, applied structured dispositions, reconciled a retrospective's carry-forward list, and validated with the CiTestInventoryTests parser. Mirror its structure (rollup heading, per-entry blocks, "preserve historical prose" rule, no submodule churn). [Source: 15-5-deferred-register-triage-sweep.md]

### Testing standards

- Tests are xUnit v3 + Shouldly. The only test relevant here is `CiTestInventoryTests` (deferred-work parser). No new tests are required for a governance sweep, but the parser must stay green. [Source: project-context.md Testing Rules]
- Running tests in this sandbox: plain `dotnet test` fails (VSTest `SocketException (13)`). Build, then `DiffEngine_Disabled=true dotnet exec` the xUnit v3 test dll, filtering to `CiTestInventoryTests`. This is the Epic 17/18 carry-forward workaround. [Source: project memory "Running .NET tests in this sandbox"]

### Line endings (avoid whole-file churn — Epic 18 Action Item 1)

- This story file and `deferred-work.md` are **CRLF** (`.editorconfig` `end_of_line = crlf`). Keep them CRLF; an editor that rewrites them to LF produces a whole-file diff and a review finding.
- `sprint-status.yaml` is currently **LF** in the working tree — preserve LF when editing it; do not flip it to CRLF.
- [Source: .editorconfig lines 3-9; `file`/`cat -A` on the three targets; project memory "CRLF line endings required"]

### Project Structure Notes

- Edited files all live under `_bmad-output/implementation-artifacts/`: `deferred-work.md` (canonical deferred register), `sprint-status.yaml` (status + `action_items:` list), and this story file `19-1-deferred-register-active-entry-classification-sweep.md`.
- Sprint-status edit targets (verify line numbers before editing — the file changes between runs): `epic-19` and `19-1-deferred-register-active-entry-classification-sweep` are updated by the create-story workflow itself (epic-19 → `in-progress`, 19.1 → `ready-for-dev`); the dev-story phase only touches the `action_items:` list per Task 4. `development_status:` spans the epic/story rows; `action_items:` is a separate top-level list with the Epic 17/18 retrospective items.
- No conflicts with the unified project structure: this story adds a Markdown rollup section and edits a YAML list; it introduces no new code paths, namespaces, or packages.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic-19 / Story-19.1 (lines 3726-3758) — story statement and the four ACs]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-30.md — Epic 19 intent, Story 19.1 scope boundaries, Deferred Register Backlog Home Rollup, success criteria]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md#Schema-for-Active-Entries and #Deferred-Register-Backlog-Home-Rollup-2026-06-30; structured entries `15.2-RV1..9`, `12.4-RV20`, `15.1-RV1..16`, `MEM-1`, `MEM-2-ASPIRATE`, `MEM-3-OPENAPI`, `1.1-RR3`]
- [Source: _bmad-output/implementation-artifacts/epic-18-retro-2026-06-25.md#Action-Items (Item 4 parked carry-forwards; Item 2 architecture anchors)]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#action_items (Epic 18 parked-carry-forwards item) and #STATUS-DEFINITIONS]
- [Source: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs — deferred-work parser contract]
- [Source: _bmad-output/implementation-artifacts/15-5-deferred-register-triage-sweep.md and 14-5-deferred-register-governance-and-sprint-status-hygiene.md — pattern precedents]
- [Source: _bmad-output/project-context.md — repo-wide rules (CRLF, central package management, submodule discipline, no production-code change here)]

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Opus 4.8, 1M context) — BMAD dev-story workflow.

### Debug Log References

- Active-entry inventory confirmed by full read of `deferred-work.md` + a Python emulation of the `CiTestInventoryTests` structured-field parser: 30 active entries (9 `open` = `15.2-RV1`…`15.2-RV9`; 21 `carried-forward` = `12.4-RV20`, `15.1-RV1`…`15.1-RV16`, `1.1-RR3`, `MEM-1`, `MEM-2-ASPIRATE`, `MEM-3-OPENAPI`).
- MEM-1 disposition cross-checked against `18-1-…md` (Completion Notes: `AppHostProjectResolutionTests` + `public-surface-stability.md`; Mcp `PackageId` is pack-time, non-reflectable, review-enforced only), the Epic 18 retro (line 46), and the sprint change proposal (line 33).
- Parser regression guard: `DiffEngine_Disabled=true dotnet exec Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` → **Total 48, Failed 0** (plain `dotnet test` avoided per the VSTest `SocketException (13)` sandbox workaround). Includes the two real-repo parses of `deferred-work.md`.
- Diff hygiene: `git diff --ignore-cr-at-eol -U0` shows exactly 3 content hunks (new section +68; `1.1-RR3` 1 line; `MEM-1` 3 lines). 53 pre-existing lone-LF lines were restored to LF after an over-broad normalization, leaving a surgical diff (no whole-file CRLF churn). `git diff --check` reports CR-as-trailing-whitespace on the new CRLF lines — the expected, documented behavior for this repo's missing root `.gitattributes` (deferred entry `15.5-RV1`; Epic 17 AI6 / Epic 18 AI1); no genuine trailing space/tab exists.

### Completion Notes List

**Final disposition of all 30 active structured entries (AC1):**

- **Scheduled story (28 entries; existing blocks untouched, byte-for-byte):** `15.2-RV1`…`15.2-RV9` → Story 19.4; `12.4-RV20` + `15.1-RV1`…`15.1-RV16` → Story 19.3; `MEM-2-ASPIRATE` + `MEM-3-OPENAPI` → Story 19.2.
- **`1.1-RR3` (residual decision):** kept `carried-forward`; owner (AppHost/release maintainer) named in prose inside the `Rationale:` line per AC1's "explicit trigger **and owner**"; existing CS0618/Aspire-14.x trigger preserved.
- **`MEM-1` (residual decision):** kept `carried-forward` (NOT resolved). Story 18.1 (done) test-enforced 5 of 6 contract items (symbol resolution + Server/Mcp assembly name + root namespace + Aspire symbol shape), but the Mcp `PackageId` half is a pack-time NuGet property not reflectable from a built assembly, so it stays review-enforced and is the genuine residual. Refreshed `Target artifact` (→ `public-surface-stability.md` + the 18.1 story), `Re-open trigger` (PackageId rename without `BREAKING CHANGE:` / a pack-time guard arriving), and `Rationale` (owner named); referenced Story 18.1 without reopening Epic 18 (AC3).
- **Three NEW accepted-debt entries (AC2; existed only in Epic 18 retro prose):** `18.4-REDIS-RACE`, `18.8-DAPR-SMOKE`, `18.4-TOKEN-EDGE` — all `Status: accepted`, `Source story: 19-1-…`, each with a re-open trigger and a `Rationale:`; placed in the new `## Story 19.1 Classification Sweep (2026-06-30)` rollup section.
- **Anti-over-promotion (AC1):** the rollup explicitly separates "schedule now" (the 28 routed entries, owned by 19.2/19.3/19.4) from "accept until trigger" (`1.1-RR3`, `MEM-1`, and the 3 new entries). The `15.3-RV*` migration-marker items are legacy prose (no field block) and were left out of scope for Story 19.4.

**Action Item 4 decision (AC2/AC4):** set Epic 18 Action Item 4 → `done` with a dated Story-19.1 note. All five parked carry-forwards now have homes — `MEM-2-ASPIRATE`/`MEM-3-OPENAPI` are story ids (Story 19.2); `18.4-REDIS-RACE`/`18.8-DAPR-SMOKE`/`18.4-TOKEN-EDGE` are accepted-debt entries with re-open triggers. No other action item was changed (AI2 architecture-anchor reconciliation left `open`; this sweep does not rewrite `architecture.md`).

**Validation (AC1):** `CiTestInventoryTests` 48/48 green; every new/edited block has exactly one `ID`/`Status`/`Source story`/`Target artifact`/`Re-open trigger` + one `Rationale` (zero `Evidence`), `Status` from the closed lowercase vocabulary; new IDs match `StructuredIdShape`. No `src/**` production changes and no submodule-pointer changes were introduced by this sweep. (`references/Hexalith.Tenants` was already modified at session start, and `2-7-…md` was modified by the concurrent story-automator — both are external to this governance sweep.) `deferred-work.md` and the story file are CRLF; `sprint-status.yaml` preserved as LF.

**Validation limits:** none blocking. The SDK parser lane ran, so the `git diff --check` fallback was not required (but `--check` was run and is clean of genuine whitespace). Docker/Aspire infra lanes remain unavailable in this sandbox but are not exercised by a planning/governance sweep.

### File List

- `_bmad-output/implementation-artifacts/deferred-work.md` (modified) — new `## Story 19.1 Classification Sweep (2026-06-30)` rollup section incl. 3 new accepted-debt blocks (`18.4-REDIS-RACE`, `18.8-DAPR-SMOKE`, `18.4-TOKEN-EDGE`); in-place field edits to `1.1-RR3` and `MEM-1`.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified) — `19-1-…` → `in-progress` then `review`; Epic 18 Action Item 4 → `done` with dated note; senior review synced Story 19.1 → `done`.
- `_bmad-output/implementation-artifacts/19-1-deferred-register-active-entry-classification-sweep.md` (modified) — `baseline_commit` frontmatter; task checkboxes; Dev Agent Record; Senior Developer Review; Change Log; Status → done.

## Senior Developer Review (AI)

Reviewer: Codex on 2026-06-30

Outcome: Approved after automatic fixes. No critical issues remain.

Findings fixed:

- [MEDIUM] Story status was still `review` after the accepted/deferred classifications and parser validation were complete. Fixed by setting this story's `Status` to `done`.
- [MEDIUM] Sprint tracking still showed `19-1-deferred-register-active-entry-classification-sweep: review`, so the workflow status was not synced after review. Fixed in `sprint-status.yaml`.
- [LOW] The story lacked the required `Senior Developer Review (AI)` record and review changelog entry. Fixed here.

Validation performed:

- AC1: verified the Story 19.1 rollup classifies the 30 active structured entries: 28 routed to Stories 19.2/19.3/19.4, `1.1-RR3` and `MEM-1` retained as carried-forward with owner/trigger/rationale, and the three Epic 18 retro prose items added as accepted-debt entries.
- AC2: verified all five Epic 18 parked carry-forwards have homes: `MEM-2-ASPIRATE`/`MEM-3-OPENAPI` route to Story 19.2; `18.4-REDIS-RACE`, `18.8-DAPR-SMOKE`, and `18.4-TOKEN-EDGE` are accepted-debt entries with re-open triggers.
- AC3: verified completed Epic 15 and Epic 18 rows remain `done`; the sweep references completed source stories rather than reopening history.
- AC4: verified Epic 18 Action Item 4 is `done` and Action Item 2 remains `open`.
- Parser guard: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Release/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` -> Total 48, Failed 0.

## Change Log

| Date | Phase | Summary |
|---|---|---|
| 2026-06-30 | dev-story | Classified all 30 active `open`/`carried-forward` structured entries: 28 affirmed scheduled-story; `1.1-RR3` and `MEM-1` re-decided in place (kept carried-forward, owner/trigger/rationale refreshed); 3 new accepted-debt entries created for the Epic 18 retro Action Item 4 prose items. Epic 18 Action Item 4 → `done`. Parser guard `CiTestInventoryTests` 48/48 green. No production/submodule changes. Story → review. |
| 2026-06-30 | senior-review | Auto-fixed review bookkeeping: story status → `done`, sprint-status Story 19.1 → `done`, Senior Developer Review notes appended. Parser guard remained 48/48 green. |
