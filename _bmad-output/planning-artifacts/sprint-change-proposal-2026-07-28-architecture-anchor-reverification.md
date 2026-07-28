---
change_trigger: "Correct-course action requested reconciling four architecture.md anchors against live code: MemoryUnitId opaque/GUID-derived (not ULID); tests xunit.v3 3.2.2 (not 2.9.3); routes minimal-API app.MapX (not Controllers/*Controller.cs); Server Dapr app-id default memories (not memories-server)"
mode: batch
status: proposed
requested_by: Administrator
approved_by: pending
project: Hexalith.Memories
date: 2026-07-28
scope_classification: minor
supersedes: null
follows:
  - sprint-change-proposal-2026-07-16-architecture-anchor-reconciliation.md
---

# Sprint Change Proposal: Architecture Anchor Re-Verification (Duplicate Trigger)

Date: 2026-07-28
Project: Hexalith.Memories
Scope: Minor — no artifact change proposed. This proposal records a fresh re-verification and
closes the trigger as a duplicate of an already-approved correction.

## 1. Issue Summary

A correct-course action requested reconciling four allegedly stale anchors in
`_bmad-output/planning-artifacts/architecture.md` against live code. The same four anchors were
the subject of `sprint-change-proposal-2026-07-16-architecture-anchor-reconciliation.md`
(approved 2026-07-16), which found the corrections already applied by commit `edcdade8`
(2026-06-25). Repository inspection on 2026-07-28 confirms the architecture still carries all
four corrected anchors, `architecture.md` has no working-tree diff, and its most recent commit
is `13cf0cbb` (2026-07-27). The trigger is therefore a re-surfaced duplicate, not a regression.

## 2. Re-Verification Evidence (2026-07-28)

Each claim was re-derived from the current repository, with a re-runnable command per
`_bmad/custom/epic-ac-verification.md` conventions. All four verdicts are **confirmed** —
the architecture already states the corrected fact.

### 2.1 Memory Unit identifier is opaque and GUID-derived, not a ULID

- `architecture.md:104` states `Id` is `string (opaque)`, produced from the workflow
  `InstanceId` (a GUID) or a fresh GUID via `ResolveMemoryUnitId`, explicitly "**not** a ULID
  and **not** time-sortable", citing `docs/dev/memory-unit-id-stability.md` (the cited doc
  exists).
- Live code: `ResolveMemoryUnitId` lives in
  `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`.
- Command: `grep -n ULID _bmad-output/planning-artifacts/architecture.md` — the only match is
  the corrective "not a ULID" text.

### 2.2 Test framework is xUnit v3 3.2.2, not 2.9.3

- `architecture.md:1226` states "**Framework:** xUnit v3 (`xunit.v3` 3.2.2), Shouldly 4.3.0,
  NSubstitute 5.3.0, coverlet.collector".
- Live pin: `references/Hexalith.Builds/Props/Directory.Packages.props:316-318` pins
  `xunit.v3`, `xunit.v3.assert`, and `xunit.v3.extensibility.core` to `3.2.2`.
- Command: `grep -n '2\.9\.3' _bmad-output/planning-artifacts/architecture.md` — no match.

### 2.3 Product REST surface is minimal-API `app.MapX`, not controllers

- `architecture.md:1518` maps "REST (external)" to "minimal-API endpoints in `Program.cs`
  (`app.MapGet/MapPost/...`)"; `architecture.md:1392-1393` places the surface under
  `Server/Endpoints/` and notes there is no `Controllers/` folder, with the EventStore
  submodule's `EventIngestionController` documented as the intentional Dapr event-intake
  exception.
- Live code: `src/Hexalith.Memories.Server/Program.cs:87-94` composes
  `app.MapIngestionEndpoints()`, `app.MapTenantLifecycleEndpoints()`, `app.MapExportEndpoints()`,
  `app.MapImportEndpoints()`, `app.MapConsistencyEndpoints()`, `app.MapCasesEndpoints()`,
  `app.MapSearchEndpoints()`, and `app.MapGraphEndpoints()`; `app.MapControllers()` at line 61
  serves only the documented EventStore adapter nuance.
- Command: `grep -n 'Controllers/' _bmad-output/planning-artifacts/architecture.md` — the only
  match is the corrective no-`Controllers/`-folder note.

### 2.4 Memories Server Dapr app-id default is `memories`, not `memories-server`

- `architecture.md:266` records app-id `memories` for the Memories Server;
  `architecture.md:1193` shows `AppId = "memories"` in the AppHost example.
- Live code: `src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs:68` defaults
  `serverName` to `"memories"` and line 123 assigns it to `DaprSidecarOptions.AppId`.
- Command: `grep -n 'memories-server' _bmad-output/planning-artifacts/architecture.md` — no
  match.

## 3. Impact Analysis

- **Epics and stories:** None. No epic or story is affected; no story is created, renamed, or
  split. The historical-slice story-scope guard binds on story authoring and registration; this
  proposal authors and registers no story, so the guard is satisfied by construction.
- **PRD:** No conflict; no change.
- **Architecture:** No conflict; the current file already states all four corrected anchors and
  has no working-tree diff.
- **UX:** Not applicable.
- **Technical:** None. No runtime path, contract, package, deployment, or test change.
- **sprint-status.yaml:** No change; no epic or story status changed.
- **deferred-work.md:** No matching open ledger entry exists for this trigger; none is created.

## 4. Recommended Approach

**Direct Adjustment — already applied; close trigger as duplicate.**

- Effort: none remaining. Risk: low. Timeline/MVP/release impact: none.
- Rollback: not justified; restoring any stale anchor would make the architecture disagree with
  live code.
- The authoritative closure record remains
  `sprint-change-proposal-2026-07-16-architecture-anchor-reconciliation.md` (approved) together
  with commit `edcdade8`. This proposal adds a dated 2026-07-28 re-verification so any sweep or
  action-item source that re-surfaces the four anchors can be pointed at a current-evidence
  record instead of re-opening the work.

## 5. Implementation Handoff

**Classification:** Minor — no implementation work exists.

**Recipient:** Developer agent (record-keeping only).

**Responsibilities:**

- Preserve the four corrected architecture anchors.
- If this trigger originated from a tracked action-item list, mark that item resolved citing
  this proposal and the 2026-07-16 proposal; do not draft further proposals for these four
  anchors unless `architecture.md` actually regresses.

**Success criteria (all currently met):**

- `architecture.md` describes Memory Unit IDs as opaque and GUID-derived — met (line 104).
- `architecture.md` identifies xUnit v3 package version 3.2.2 — met (line 1226).
- `architecture.md` maps the product REST surface to minimal APIs with the EventStore
  Dapr-controller nuance retained — met (lines 1392-1393, 1518).
- `architecture.md` records `memories` as the Memories Server Dapr app-id default — met
  (lines 266, 1193).
- PRD, epics, UX, sprint status, runtime code, and historical evidence remain unchanged — met.

## Checklist Record

### 1. Understand the trigger and context

- [x] 1.1 Trigger is a correct-course action naming four architecture anchors; no failing
  story. Prior owner of the same issue: Epic 18 retrospective action item 2.
- [x] 1.2 Core problem: suspected architecture/code drift on four anchors; categorized as a
  re-surfaced duplicate of a resolved issue.
- [x] 1.3 Evidence collected from `architecture.md`, live source, central package pins, cited
  docs, and Git history (Section 2).

### 2. Epic impact assessment

- [x] 2.1 Current sprint plan remains valid.
- [N/A] 2.2-2.5 No epic modification, invalidation, addition, or resequencing.

### 3. Artifact conflict and impact analysis

- [x] 3.1 PRD reviewed; no conflict.
- [x] 3.2 Architecture reviewed; all four anchors already corrected, file clean at HEAD.
- [N/A] 3.3 UX unaffected.
- [x] 3.4 Code, tests/packages, deployment, CI, and ledgers reviewed; no secondary edit needed.

### 4. Path forward evaluation

- [x] 4.1 Direct Adjustment viable and already applied; effort none, risk low.
- [N/A] 4.2 Rollback would restore stale guidance.
- [N/A] 4.3 PRD/MVP review unnecessary.
- [x] 4.4 Selected: Direct Adjustment recognized as applied; close as duplicate.

### 5. Sprint Change Proposal components

- [x] 5.1-5.5 Issue summary, impacts, recommendation, MVP statement, and handoff documented
  above.

### 6. Final review and handoff

- [x] 6.1-6.2 Checklist complete; proposal checked against repository evidence.
- [!] 6.3 Explicit approval pending — drafted in an autonomous session; Administrator approval
  requested. The proposal changes no artifact, so approval only ratifies the duplicate-closure
  disposition.
- [N/A] 6.4 `sprint-status.yaml` requires no update.
- [x] 6.5 Handoff is record-keeping only; no implementation remains.

## Workflow Execution Log

| Date | Event | Result |
|---|---|---|
| 2026-07-28 | Trigger received via correct-course action | Confirmed clear |
| 2026-07-28 | Collision check against same-day proposals | No overlap; prior 2026-07-16 proposal found |
| 2026-07-28 | Four anchors re-verified against architecture.md and live code | All confirmed corrected |
| 2026-07-28 | Direct Adjustment evaluated | Already applied (`edcdade8`, ratified 2026-07-16) |
| 2026-07-28 | Minor-scope handoff drafted | Record-keeping only; approval pending |
