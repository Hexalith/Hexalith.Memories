# Story 17.5: Responsive and Accessible Web Validation

Status: ready-for-dev

## Story

As a user of the future web surface,
I want trust-critical workflows to remain usable across screen sizes, keyboard, screen reader, reduced-motion, and forced-colors contexts,
so that evidence inspection is accessible and reliable rather than visual polish only.

## Acceptance Criteria

1. Given the web UI is tested at 360px, 768px, 1024px, and 1440px, when Evidence Cockpit, Evidence Packet, Source Citation Stack, Retrieval Axis Breakdown, Recovery Action Panel, Case Activity Trail, Agent Packet Inspector, and Operator Console surfaces are rendered, then scope, confidence, freshness, source count, evidence health, and recovery remain reachable, and trust-critical content does not require horizontal scrolling.
2. Given automated accessibility checks run, when the surfaces are validated, then checks cover color contrast, accessible names, form labels, ARIA validity, heading order, and focusable controls.
3. Given human accessibility checks run, when keyboard-only navigation, focus order, no-color-only state comprehension, reduced motion, high contrast, and at least one screen reader pass are tested, then critical trust workflows remain usable and defects are tracked before release.
4. Given overlays, dialogs, drawers, source previews, graph detail panels, MCP inspectors, and confirmations are opened, when focus moves, then focus enters the overlay predictably and returns to the invoking control when closed.
5. Given source preview, graph detail, recovery action, tooltip, command, or status behavior is trust-critical, when the UI is used by keyboard or touch, then no behavior depends on hover-only interaction.
6. Given accessible labels, tooltips, announcements, copied text, diagnostics, or error payloads are emitted, when they contain tenant or source context, then secrets, raw payloads, bearer tokens, tenant-sensitive diagnostics, and restricted source details are not exposed.

## Tasks / Subtasks

- [ ] Task 0 - Confirm validation scope, dependencies, and runnable surfaces (AC: 1-6)
  - [ ] Confirm Story 2.7 has landed the canonical `Contracts.V1` Evidence Packet contract and fixture semantics. If it is not `done`, use approved contract fixtures only and do not patch Story 2.7 source, tests, CLI, MCP, or mapper files from this story.
  - [ ] Read Stories 17.1 through 17.4 before implementation. Story 17.5 validates those surfaces; it must not invent new Evidence Packet, recovery, filter, lens, benchmark, operator-health, or MCP schema semantics.
  - [ ] Identify every runnable Memories/FrontComposer web surface, specimen, or fixture host that represents Evidence Cockpit, Evidence Packet, Source Citation Stack, Retrieval Axis Breakdown, Recovery Action Panel, Case Activity Trail, Agent Packet Inspector, and Operator Console behavior.
  - [ ] If a surface is not runnable yet, create the smallest fixture/specimen hook needed to validate existing component behavior. Do not implement new product workflows just to make the validation pass.
  - [ ] Read `Hexalith.FrontComposer/_bmad-output/project-context.md`, `Hexalith.FrontComposer/tests/README.md`, and `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Testing/README.md` before adding bUnit or Playwright coverage.
  - [ ] Verify the local Fluent UI Blazor package in `Hexalith.FrontComposer/Directory.Packages.props` before copying examples. The local package is `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.2-26098.1`; the available MCP documentation is for `5.0.0.26098`, so local code/tests are authoritative when signatures differ.

- [ ] Task 1 - Add responsive viewport validation for trust-critical surfaces (AC: 1)
  - [ ] Validate 360px mobile, 768px tablet, 1024px desktop, and 1440px wide desktop behavior for every implemented Epic 17 surface or specimen.
  - [ ] Assert that scope, confidence, freshness, source count, evidence health, affected capability, and safest recovery action remain visible or keyboard/touch reachable at every viewport.
  - [ ] Assert trust-critical content does not require horizontal-scroll-only access. Overflow may use drawers, tabs, row details, disclosure regions, stacked layouts, or command menus when keyboard and touch reachable.
  - [ ] Validate compact behavior for data-heavy regions: source citations, axis breakdowns, graph summaries, activity trails, ingestion stages, health matrices, benchmark results, and MCP schema/JSON views.
  - [ ] Use FrontComposer viewport and density conventions where available, including tablet/phone comfortable density behavior and existing layout breakpoint state.
  - [ ] Do not introduce a separate responsive breakpoint taxonomy unless FrontComposer lacks a needed concept; record any gap as a deferred design decision.

- [ ] Task 2 - Add automated accessibility gates (AC: 2, 4, 5, 6)
  - [ ] Reuse `Hexalith.FrontComposer/tests/e2e/helpers/a11y.ts` and the Playwright `data-testid` contract where browser coverage exists.
  - [ ] Cover color contrast, accessible names, form labels, ARIA validity, heading order, focusable controls, and zero target-node failures in automated checks.
  - [ ] Use `data-testid`, accessible role, or label selectors. Do not use CSS class selectors, arbitrary text selectors for framework behavior, committed sleeps, or previous-test state.
  - [ ] Add bUnit coverage with `Hexalith.FrontComposer.Testing`, `FrontComposerTestBase`, or `BunitContext` plus `AddFluentUIComponents()` for component-level labels, roles, live-region attributes, focus sentinels, and sanitized markup.
  - [ ] Keep axe findings bounded and reviewable. Store only sanitized, relative-path-safe artifacts; do not publish raw payloads, tenant identifiers, local absolute paths, secrets, stack traces, or unrestricted page dumps.

- [ ] Task 3 - Validate keyboard, focus, touch, and no-hover behavior (AC: 3, 4, 5)
  - [ ] Define one focus contract per interactive surface: initial focus target, tab order, escape/cancel behavior, activation behavior, focus return, and screen-reader announcement.
  - [ ] Test keyboard-only paths through scope selection, query or command entry, filter updates, grid sorting/filtering, source preview, axis details, graph detail, recovery action, command palette, confirmation, and return navigation.
  - [ ] Validate drawers, dialogs, source previews, graph detail panels, MCP inspectors, benchmark detail panels, health detail panels, and confirmations move focus inside on open and return focus to the invoking control on close.
  - [ ] Ensure source preview, graph detail, recovery action, tooltip-critical command, status explanation, copy action, export action, and row action behavior works by keyboard and touch. Hover may enhance but must not be required.
  - [ ] Use appropriate live-region behavior: `polite` for non-blocking updates, assertive or alert semantics only for blocking or safety-critical trust states.

- [ ] Task 4 - Validate reduced-motion, forced-colors, and screen-reader readability (AC: 3)
  - [ ] Reuse existing FrontComposer reduced-motion and forced-colors E2E patterns, including `page.emulateMedia({ reducedMotion: 'reduce' })` and forced-colors browser context checks where supported.
  - [ ] Prove confidence, freshness, evidence health, scope, degraded backend, destructive action, selected row, active filter, and recovery state comprehension does not depend on color, animation, icon shape, chart position, or timeline position alone.
  - [ ] Validate text equivalents for charts, matrices, timelines, score bars, graph summaries, JSON/schema views, and status badges.
  - [ ] Include at least one documented screen-reader pass or equivalent manual accessibility checklist for the trust workflow selected by the implementation team.
  - [ ] Track any unresolved human accessibility defects before release; do not mark validation complete on automated axe pass alone when manual checks fail.

- [ ] Task 5 - Add sanitization and privacy assertions for accessibility surfaces (AC: 6)
  - [ ] Assert visible text, accessible names, tooltips, live announcements, copied text, exported snippets, diagnostics, logs, snapshots, and axe artifacts do not expose secrets, bearer tokens, raw payloads, serialized packets, tenant-sensitive diagnostics, local absolute paths, restricted source details, provider internals, stack traces, or unsanitized exception text.
  - [ ] Reuse Story 2.7 and Epic 17 canonical fixtures for happy, degraded, unauthorized, redacted, omitted/compressed, stale, invalid/schema-mismatch, cross-tenant, and missing-source packets.
  - [ ] Include tenant-isolation cases proving accessible labels and copy/export payloads do not disclose whether evidence exists outside the current authorization scope.
  - [ ] Treat accessibility labels and diagnostics as security surfaces equal to visible UI. Do not build them from raw JSON, exception text, local paths, DOM text reconstruction, or backend diagnostic dumps.

- [ ] Task 6 - Produce validation evidence and release guardrails (AC: 1-6)
  - [ ] Add an AC-to-evidence matrix listing each surface, viewport, automated check, keyboard path, focus contract, reduced-motion/forced-colors result, screen-reader/manual result, sanitization assertion, and defect disposition.
  - [ ] Store validation artifacts under existing test artifact or Playwright output conventions. Keep artifacts bounded, sanitized, relative-path-safe, and excluded from commits unless the repo already tracks that class of fixture evidence.
  - [ ] Run focused bUnit/unit tests for changed Memories web or FrontComposer component/test projects.
  - [ ] Run focused Playwright/E2E validation when a runnable surface exists. If no runnable web surface exists for a required pattern, record the missing fixture as a blocking or deferred evidence item instead of inventing a product flow.
  - [ ] Run `git diff --check`.

## Dev Notes

### Current Implementation State

- Epic 17 is future web UI scope. Story 17.5 is a validation and quality-gate story for the web surfaces described by Stories 17.1 through 17.4; it should not pull unrelated MVP work forward or weaken CLI/MCP Evidence Packet behavior.
- Story 2.7 currently owns the shared Evidence Packet contract and canonical fixture semantics. Story 17.5 consumes that contract for validation and must not redefine confidence, freshness, evidence health, scope, omitted detail, source, graph, recovery, benchmark, activity, operator-health, or MCP packet semantics.
- Stories 17.1, 17.2, 17.3, and 17.4 define the Evidence Cockpit, recovery state grammar, interaction patterns, and role-specific lenses this story validates.
- FrontComposer already contains relevant validation foundations: `Hexalith.FrontComposer.Testing`, bUnit shell tests, `AddFluentUIComponents()`, Playwright E2E fixtures, axe helper, `data-testid` selector policy, viewport/density behavior, forced-colors/reduced-motion specs, command palette, data-grid, dialog, navigation, and diagnostics patterns.
- FrontComposer is a root-level submodule in this application repository. Treat submodule edits as intentional and separately reviewable. Do not initialize nested submodules or run recursive submodule updates.

### Validation Semantics

- Accessibility is product correctness for trust workflows. A user who cannot reach scope, confidence, freshness, source, evidence health, affected capability, or recovery state cannot safely use the evidence model.
- Automated accessibility checks are necessary but insufficient. This story requires manual or human-equivalent validation of keyboard-only use, focus order, no-color-only comprehension, reduced motion, high contrast, and at least one screen-reader pass.
- Responsive validation must use the Epic 17 viewport set: 360px, 768px, 1024px, and 1440px. These align with the UX specification's mobile, tablet, desktop, and wide desktop breakpoints.
- Trust-critical content may move into compact affordances, but it must remain reachable without horizontal-scroll-only access and without hover-only behavior.
- Validation should fail closed. Missing runnable fixtures, missing selectors, empty axe scans, unsanitized artifacts, or inaccessible trust-critical paths are defects, not skipped polish.

### Testing Notes

- Component and state tests should use xUnit, Shouldly, bUnit, NSubstitute, and existing FrontComposer helpers.
- Browser/accessibility validation should use Playwright with role/label or `data-testid` selectors, `@axe-core/playwright`, observable waits, and existing E2E fixture composition.
- Do not use CSS class selectors, arbitrary sleeps, previous-test state, broad screenshots as the only proof, or generated artifacts that contain machine-local paths.
- If implementation changes FrontComposer submodule files, run tests from the submodule root and keep those changes scoped to the validation story.
- If a Memories web project is introduced, keep package versions centralized and avoid inline `Version` attributes in `.csproj` files.

### Dependencies and Non-Goals

- Dependency: Story 2.7 Evidence Packet Contract Mapping must provide canonical contract semantics and fixtures. If it is not done at dev time, this story should validate only approved fixtures and pause product-surface validation that depends on missing contract behavior.
- Dependency: Story 17.1 should define or establish Evidence Cockpit and trust components.
- Dependency: Story 17.2 should define or establish recovery and feedback state grammar.
- Dependency: Story 17.3 should define or establish contract-aware web interaction patterns.
- Dependency: Story 17.4 should define or establish role-specific inspection lenses.
- Non-goal: no new Evidence Packet contract semantics, retrieval algorithm, recovery action semantics, web product workflow, benchmark algorithm, MCP schema grammar, operator health taxonomy, or FrontComposer framework redesign.
- Non-goal: no broad Fluent UI package upgrade, no new assertion/test framework, no nested submodule initialization, and no recursive submodule update.
- Deferred decisions: exact release-blocking threshold for manual screen-reader defects, final artifact retention policy for accessibility evidence, mobile grid-to-card transformation strategy, and any unsupported browser/assistive-technology matrix beyond the initial validation set require product or architecture approval unless already defined upstream.

### Suggested Validation Commands

```powershell
dotnet test Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --filter "FullyQualifiedName~Accessibility|FullyQualifiedName~Navigation|FullyQualifiedName~DataGrid|FullyQualifiedName~Dialog|FullyQualifiedName~CommandPalette|FullyQualifiedName~Density"
npm --prefix Hexalith.FrontComposer/tests/e2e test -- --grep "accessibility|responsive|density"
git diff --check
```

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 17 and Story 17.5 acceptance criteria.
- `_bmad-output/planning-artifacts/ux-design-specification.md` - responsive breakpoints, WCAG target, trust-state accessibility, keyboard/touch, forced-colors, reduced-motion, and validation guidance.
- `_bmad-output/planning-artifacts/architecture.md` - `Contracts.V1`, Evidence Packet ownership, tenant isolation, structured errors, and cross-surface contract boundaries.
- `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md` - prerequisite Evidence Packet contract and fixture semantics.
- `_bmad-output/implementation-artifacts/17-1-evidence-cockpit-and-trust-components.md` - Evidence Cockpit, Trust Strip, Scope Header, source, axis, and graph guardrails.
- `_bmad-output/implementation-artifacts/17-2-recovery-and-feedback-state-grammar.md` - recovery, feedback, conflict, compression, accessibility, and sanitization guardrails.
- `_bmad-output/implementation-artifacts/17-3-contract-aware-web-interaction-patterns.md` - form, filter, navigation, command, overlay, data-grid, focus, and tenant-scope guardrails.
- `_bmad-output/implementation-artifacts/17-4-role-specific-web-inspection-lenses.md` - role-specific lenses and field-trace guardrails.
- `_bmad-output/project-context.md` - Memories project rules for .NET, contracts, tests, warnings-as-errors, and submodules.
- `Hexalith.FrontComposer/_bmad-output/project-context.md` - FrontComposer-specific implementation, accessibility, testing, and submodule rules.
- `Hexalith.FrontComposer/tests/README.md` - bUnit, Playwright, axe, selector, artifact, and E2E conventions.
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Testing/README.md` - component test host utilities.
- `Hexalith.FrontComposer/Directory.Packages.props` - local Fluent UI Blazor package version.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Created from sprint status backlog item `17-5-responsive-and-accessible-web-validation`.
- Loaded preflight JSON, automation memory, sprint status, story lessons, project context, Epic 17 requirements, UX responsive/accessibility sections, Story 2.7 artifact, Stories 17.1 through 17.4 artifacts, FrontComposer project context, FrontComposer package/test/E2E context, recent git history, and Fluent UI Blazor MCP version compatibility warning.
- No product code implementation performed in this create-story workflow.

### Completion Notes List

- Ready-for-dev story created on 2026-05-20.
- Scope is limited to responsive, accessibility, focus, reduced-motion, forced-colors, screen-reader, and sanitization validation for future web Evidence Packet surfaces.
- Story explicitly records Story 2.7 and Stories 17.1 through 17.4 dependencies, local FrontComposer validation hooks, Epic 17 viewport coverage, manual accessibility evidence, and the local Fluent UI Blazor version mismatch with the MCP documentation source.

### File List

- `_bmad-output/implementation-artifacts/17-5-responsive-and-accessible-web-validation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/process-notes/predev-hardening-runs.log`

## Change Log

- 2026-05-20: Created ready-for-dev story artifact for Responsive and Accessible Web Validation.

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.
