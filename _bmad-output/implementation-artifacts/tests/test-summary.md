# Test Automation Summary — Story 17.2 (Recovery and Feedback State Grammar)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Feature under test:** Memories Web recovery state grammar — `RecoveryStateMapper`,
  `MemoriesRecoveryActionPanel`, and its composition inside `MemoriesEvidenceCockpit`.
- **Date:** 2026-06-24
- **Framework detected:** xUnit v3 (`xunit.v3` 3.2.2) + bUnit (2.8.4-preview) + Shouldly + FrontComposer
  `Hexalith.FrontComposer.Testing` (`FrontComposerTestBase`). Matched the project's existing test stack;
  no new framework introduced.
- **Run command (sandbox):** built with serialized MSBuild (`-m:1`); executed the xUnit v3 in-process
  executable directly with `DiffEngine_Disabled=true` (project `dotnet test`/VSTest socket is blocked in
  this sandbox, per the story's Dev Agent Record).

## Result

| | Tests | Errors | Failed | Skipped |
|---|---|---|---|---|
| Baseline (existing) | 75 | 0 | 0 | 0 |
| **After gap auto-apply** | **100** | **0** | **0** | **0** |

All 100 tests pass. `git diff --check` is clean for every file touched by this workflow. The
`Hexalith.Memories.Web` source project builds clean under `TreatWarningsAsErrors=true` (0/0).

## Gaps discovered and auto-applied

The feature already shipped with focused tests. This pass mapped Story 17.2's acceptance criteria and
Task 5 test requirements against existing coverage and filled the remaining gaps. **+25 test cases.**

### Mapper coverage — `Components/Recovery/RecoveryStateMapperGapTests.cs` (new)

- **G1 — Exhaustive state × isolation precedence/safety sweep.** Cross-product of every
  `EvidencePacketState` × `EvidencePacketIsolationStatus`: asserts the mapper never throws, is
  deterministic, always traces to named contract sources, never emits `WrongCase`, collapses every
  restrictive/unauthorized scope to `Unauthorized`, suppresses risk markers + omitted-detail hints under
  restrictive scope, and never leaks count-bearing clue axes when unauthorized. (Task 5 "exhaustive
  state/precedence matrix … unknown/future enum values"; AC3 side-channel safety.)
- **G2 — Stale + compressed combination.** `StaleMemory` stays primary with a `compressed` secondary
  risk marker and the omitted detail group remains visible. (Task 5 "stale/compressed/conflict
  combinations".)
- **G3 — Stale + degraded + sources combination.** Conflict wins precedence (`Conflicting`) while
  staleness remains a visible `stale` risk marker. (Task 5 combinations; AC3 no-confident-answer.)
- **G4 — Sanitization sweep over every fixture.** Flattens all dynamic view-model strings (clue, tenant,
  case, omitted names, expansions, action labels/guidance/targets) and proves no fixture — including the
  sensitive ones — leaks bearer tokens, local paths, connection strings, JWTs, or secrets. (Task 5
  negative-leakage, broadened from 2 fixtures to all.)
- **G5 — Whitelisted diagnostic-clue shape over every fixture.** Every state yields a non-empty clue
  matching the `code=token` whitelist shape. (AC1 diagnostic clue.)

### Component coverage — `Components/Recovery/RecoveryActionPanelStateGrammarTests.cs` (new)

- **G6 — Per-state full grammar render (14 theory cases).** Renders the panel for every actionable state
  (weak, stale, degraded ×2, conflicting ×2, no-match, not-ingested, graph-gap, insufficient ×2,
  compressed, unauthorized, unknown) and asserts each shows title, explanation, diagnostic clue, a
  text-bearing severity badge, and a text-bearing affected-capability badge — never color alone — with no
  sensitive markup leak. (Task 5 "each state renders title, explanation, diagnostic clue, severity,
  affected capability"; AC4 color-is-never-the-only-signal.)
- **G7 — State-transition accessibility (4 tests).** Re-renders across packet changes:
  unauthorized→allowed (assertive `alert` → polite `status`), complete→degraded (hidden → conflicting),
  conflicting→resolved (shown → hidden), compressed→expanded (omitted-detail grammar dropped). (Task 4 +
  Advanced Elicitation transition coverage: loading→no-result, unauthorized→allowed, complete→degraded,
  compressed→expanded, conflicting→resolved.)

### Integration coverage — `Components/Evidence/EvidenceCockpitRecoveryTransitionTests.cs` (new)

- **G8 — Loading→result transition.** While loading the recovery panel is absent; once a packet arrives
  the panel appears for the resolved state. (Task 4 loading→result transition at the cockpit boundary.)
- **G9 — Dual-announcement wiring.** An unauthorized packet renders the assertive restrictive banner
  (`role=alert`) alongside the recovery panel announced politely (`role=status`, `AnnounceAssertively=false`)
  so the two live regions do not compete; only the safe `CheckAuthorization` action surfaces and no
  restricted content leaks. (Task 2 routing + AC4 announcements.)

### Supporting fixtures — `Components/Recovery/RecoveryPacketFixtures.cs` (extended)

- Added `StaleAndCompressed()` and `StaleDegradedWithSources()`, built on the canonical Story 2.7-aligned
  Evidence Packet fixtures, for the new combination tests.

## Coverage map (Story 17.2)

- **AC1** (state grammar: title/explanation/clue/severity/capability/safest action): G6 (all states) +
  existing panel tests.
- **AC2** (no-result distinctions): existing state matrix + G1 sweep; `WrongCase` proven unreachable.
- **AC3** (conflict not smoothed into a confident answer): G1, G3 + existing disagreement tests.
- **AC4** (keyboard/AT readable, color never the only signal, transition announcements): G6, G7, G8, G9 +
  existing keyboard/localization tests.

## Notes / scope boundaries

- No API endpoint tests apply — this is a Razor Component Library slice with no new HTTP surface; the
  mapper unit/contract tests are the API-equivalent layer.
- No runnable web host exists for this RCL-only slice, so Playwright/axe viewport validation remains not
  applicable (consistent with the story's Dev Agent Record); component-level accessibility is asserted via
  semantic roles, `aria-live`, and accessible names in bUnit.
- The pre-existing `git diff --check` trailing-whitespace warnings are in unrelated Story 2.7 markdown/YAML
  artifacts modified before this task; no file changed by this workflow has whitespace errors.

## Next steps

- Run the Web test lane in CI alongside the rest of the solution.
- When a runnable web host lands (later Epic 17 stories), add Playwright + axe viewport checks at 360/768/
  1024/1440px to complete Task 6's browser-level validation.
