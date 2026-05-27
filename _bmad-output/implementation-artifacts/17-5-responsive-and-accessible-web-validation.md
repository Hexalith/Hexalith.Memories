# Story 17.5: Responsive and Accessible Web Validation

Status: ready-for-dev

## Story

As a user of the future web surface,
I want trust-critical workflows to remain usable across screen sizes, keyboard, screen reader, reduced-motion, and forced-colors contexts,
so that evidence inspection is accessible and reliable rather than visual polish only.

## Acceptance Criteria

1. Given the web UI is tested at 360px, 768px, 1024px, and 1440px, when Evidence Cockpit, Evidence Packet, Source Citation Stack, Retrieval Axis Breakdown, Recovery Action Panel, Case Activity Trail, Ingestion Lifecycle Tracker, Operator Health Matrix, Benchmark Result Comparator, Agent Packet Inspector, and any existing operator-health surfaces are rendered, then scope, confidence, freshness, source count, evidence health, and recovery remain reachable, and trust-critical content does not require horizontal scrolling.
2. Given automated accessibility checks run, when the surfaces are validated, then checks cover color contrast, accessible names, form labels, ARIA validity, heading order, and focusable controls.
3. Given human accessibility checks run, when keyboard-only navigation, focus order, no-color-only state comprehension, reduced motion, high contrast, and at least one screen reader pass are tested, then critical trust workflows remain usable and defects are tracked before release.
4. Given overlays, dialogs, drawers, source previews, graph detail panels, MCP inspectors, and confirmations are opened, when focus moves, then focus enters the overlay predictably and returns to the invoking control when closed.
5. Given source preview, graph detail, recovery action, tooltip, command, or status behavior is trust-critical, when the UI is used by keyboard or touch, then no behavior depends on hover-only interaction.
6. Given accessible labels, tooltips, announcements, copied text, diagnostics, or error payloads are emitted, when they contain tenant or source context, then secrets, raw payloads, bearer tokens, tenant-sensitive diagnostics, and restricted source details are not exposed.

## Party-Mode Hardening Clarifications

- Story 17.5 is a validation story, not permission to build missing Epic 17 product workflows. If Story 2.7 or Stories 17.1 through 17.4 are not `done` at implementation time, validate only already-implemented surfaces, approved fixtures, or specimen hosts and record missing product surfaces as `fixture-only`, `blocked`, or `deferred`.
- Fixture/specimen validation must not be reported as full product-surface validation. The evidence matrix must distinguish runnable product routes from fixture-only component/specimen coverage.
- Before adding tests, produce a surface inventory with `surface`, `upstream story/source`, `implementation source`, `runnable route or specimen`, `fixture family`, `selector/testid anchors`, `validation level`, and `blocked/deferred reason`.
- FrontComposer changes must stay validation-focused and backwards-compatible. Do not add or change public APIs, source generators, package versions, framework primitives, product workflows, Evidence Packet semantics, recovery semantics, or operator-console shell behavior unless this story is explicitly re-scoped.
- Browser validation should be bounded to smoke paths per runnable surface and required viewport. State matrices, negative permutations, redaction cases, and contract-edge fixtures belong primarily in bUnit/unit tests unless cross-surface behavior requires Playwright.
- The accessibility target is WCAG 2.2 AA where supported. Existing axe helpers that cover only WCAG 2.0/2.1 tags must either add supported WCAG 2.2 checks or emit a documented gap covered by manual evidence.
- Accessibility evidence is a release artifact and a security surface. Axe summaries, screenshots, traces, copied text, exported snippets, diagnostics, and manual evidence must be bounded, sanitized, relative-path-safe, and covered by a redaction scan or manifest.
- Manual accessibility evidence must name the workflow, viewport, browser, OS, screen reader or checklist method, tester/date, pass/fail result, defects, severity, owner, waiver state, and release disposition.

## Advanced Elicitation Hardening Clarifications

- Validation must fail closed. A surface cannot be counted as covered unless the inventory names an implementation source, a runnable route/specimen/fixture, selectors or role/label anchors, the validation level, and the evidence row proving each required trust signal was reached.
- Responsive evidence must prove information parity, not just page rendering. Compact, disclosed, tabbed, drawer, or stacked views must expose the same canonical trust fields and recovery actions through visible text, accessible names, keyboard/touch paths, and sanitized copy/export behavior.
- Artifact redaction must use seeded canary values for bearer tokens, tenant identifiers, restricted source labels, raw payload fragments, local absolute paths, stack traces, provider internals, and cross-tenant hints. The scan must fail when a canary appears in screenshots, traces, axe summaries, snapshots, copied/exported text, diagnostics, or manual evidence.
- Tooling gaps are not product passes. Unsupported WCAG 2.2 rules, forced-colors gaps, reduced-motion gaps, browser limitations, or unavailable assistive-technology combinations must be recorded with owner, severity, waiver state, release disposition, and whether product validation remains blocked.
- Each workflow-level evidence row must include the starting state, state transition, expected focus owner, expected announcement or label, trust-critical field checked, recovery or dismissal outcome, and return-to-origin behavior so settled-state snapshots cannot mask inaccessible transitions.
- Keep the implementation decision budget small: do not resolve release-blocking thresholds, artifact retention policy, mobile grid transformation, unsupported browser/assistive-technology support, or new component semantics inside this story unless upstream product or architecture artifacts already decide them.

## Tasks / Subtasks

- [ ] Task 0 - Confirm validation scope, dependencies, and runnable surfaces (AC: 1-6)
  - [ ] Confirm Story 2.7 has landed the canonical `Contracts.V1` Evidence Packet contract and fixture semantics. If it is not `done`, use approved contract fixtures only and do not patch Story 2.7 source, tests, CLI, MCP, or mapper files from this story.
  - [ ] Read Stories 17.1 through 17.4 before implementation. Story 17.5 validates those surfaces; it must not invent new Evidence Packet, recovery, filter, lens, benchmark, operator-health, or MCP schema semantics.
  - [ ] If Stories 17.1 through 17.4 are not `done`, record each missing product surface as `fixture-only`, `blocked`, or `deferred`; do not claim product-surface validation complete from specimens alone.
  - [ ] Identify every runnable Memories/FrontComposer web surface, specimen, or fixture host that represents Evidence Cockpit, Evidence Packet, Source Citation Stack, Retrieval Axis Breakdown, Recovery Action Panel, Case Activity Trail, Agent Packet Inspector, and Operator Console behavior.
  - [ ] Produce a surface inventory table with `surface`, `upstream story/source`, `implementation source`, `runnable route/specimen`, `fixture family`, `selector/testid anchors`, `validation level`, and `blocked/deferred reason` before adding tests.
  - [ ] If a surface is not runnable yet, create the smallest fixture/specimen hook needed to validate existing component behavior. Do not implement new product workflows just to make the validation pass.
  - [ ] Define the validation claim for each surface as one of `product-route`, `component-specimen`, `contract-fixture`, `blocked`, or `deferred`; require matching evidence before marking the claim complete.
  - [ ] Read `Hexalith.FrontComposer/_bmad-output/project-context.md`, `Hexalith.FrontComposer/tests/README.md`, and `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Testing/README.md` before adding bUnit or Playwright coverage.
  - [ ] Verify the local Fluent UI Blazor package in `Hexalith.FrontComposer/Directory.Packages.props` before copying examples. The local package is `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.2-26098.1`; the available MCP documentation is for `5.0.0.26098`, so local code/tests are authoritative when signatures differ.

- [ ] Task 1 - Add responsive viewport validation for trust-critical surfaces (AC: 1)
  - [ ] Validate 360px mobile, 768px tablet, 1024px desktop, and 1440px wide desktop behavior for every implemented Epic 17 surface or specimen.
  - [ ] Assert that scope, confidence, freshness, source count, evidence health, affected capability, and safest recovery action remain visible or keyboard/touch reachable at every viewport.
  - [ ] Assert trust-critical content does not require horizontal-scroll-only access. Overflow may use drawers, tabs, row details, disclosure regions, stacked layouts, or command menus when keyboard and touch reachable.
  - [ ] Validate compact behavior for data-heavy regions: source citations, axis breakdowns, graph summaries, activity trails, ingestion stages, health matrices, benchmark results, and MCP schema/JSON views.
  - [ ] Verify sticky headers, sticky actions, drawers, dialogs, command menus, toasts, and status banners do not obscure the focused element or hide scope, confidence, freshness, evidence health, or recovery controls at the tested viewport sizes.
  - [ ] For any compact transformation, prove the canonical fields remain available through the same contract-backed values rather than duplicated display-only strings that can drift from accessible names, copy/export text, or diagnostics.
  - [ ] Use FrontComposer viewport and density conventions where available, including tablet/phone comfortable density behavior and existing layout breakpoint state.
  - [ ] Do not introduce a separate responsive breakpoint taxonomy unless FrontComposer lacks a needed concept; record any gap as a deferred design decision.

- [ ] Task 2 - Add automated accessibility gates (AC: 2, 4, 5, 6)
  - [ ] Reuse `Hexalith.FrontComposer/tests/e2e/helpers/a11y.ts` and the Playwright `data-testid` contract where browser coverage exists.
  - [ ] Cover color contrast, accessible names, form labels, ARIA validity, heading order, focusable controls, and zero target-node failures in automated checks.
  - [ ] Use required selector/testid anchors for each scanned surface and fail on zero target nodes so axe scans cannot pass against unrelated shell chrome or empty fixture roots.
  - [ ] Include WCAG 2.2 AA tags where the local axe/tooling stack supports them. If the helper is limited to WCAG 2.0/2.1 tags, record the documented WCAG 2.2 gap and cover it with manual evidence.
  - [ ] Use `data-testid`, accessible role, or label selectors. Do not use CSS class selectors, arbitrary text selectors for framework behavior, committed sleeps, or previous-test state.
  - [ ] For every automated scan, record the target selector, matched node count, route/specimen identity, fixture family, and scan tags in bounded evidence so an empty or wrong-root scan is diagnosable.
  - [ ] Add bUnit coverage with `Hexalith.FrontComposer.Testing`, `FrontComposerTestBase`, or `BunitContext` plus `AddFluentUIComponents()` for component-level labels, roles, live-region attributes, focus sentinels, and sanitized markup.
  - [ ] Keep axe findings bounded and reviewable. Store only sanitized, relative-path-safe artifacts; do not publish raw payloads, tenant identifiers, local absolute paths, secrets, stack traces, or unrestricted page dumps.
  - [ ] Validate localized visible text, accessible names, tooltip text, live announcements, copied/exported text, and diagnostics through the supported resource path. Include long-string wrapping, pseudo-localized, or French-language expansion evidence where available.

- [ ] Task 3 - Validate keyboard, focus, touch, and no-hover behavior (AC: 3, 4, 5)
  - [ ] Define one focus contract per interactive surface: initial focus target, tab order, escape/cancel behavior, activation behavior, focus return, and screen-reader announcement.
  - [ ] Test keyboard-only paths through scope selection, query or command entry, filter updates, grid sorting/filtering, source preview, axis details, graph detail, recovery action, command palette, confirmation, and return navigation.
  - [ ] Validate drawers, dialogs, source previews, graph detail panels, MCP inspectors, benchmark detail panels, health detail panels, and confirmations move focus inside on open and return focus to the invoking control on close.
  - [ ] For overlays, assert focus trap, background inertness, Escape/cancel behavior, nested overlay behavior where applicable, scroll preservation, and focus return to the invoking control.
  - [ ] Ensure source preview, graph detail, recovery action, tooltip-critical command, status explanation, copy action, export action, and row action behavior works by keyboard and touch. Hover may enhance but must not be required.
  - [ ] Include transition checks for loading-to-content, loading-to-error, degraded-to-recovered, stale-to-refreshed, compressed-to-expanded, unauthorized/forbidden-to-safe-message, and confirmation-open-to-dismissed states when the upstream fixtures expose those states.
  - [ ] Use appropriate live-region behavior: `polite` for non-blocking updates, assertive or alert semantics only for blocking or safety-critical trust states.

- [ ] Task 4 - Validate reduced-motion, forced-colors, and screen-reader readability (AC: 3)
  - [ ] Reuse existing FrontComposer reduced-motion and forced-colors E2E patterns, including `page.emulateMedia({ reducedMotion: 'reduce' })` and forced-colors browser context checks where supported.
  - [ ] Prove confidence, freshness, evidence health, scope, degraded backend, destructive action, selected row, active filter, and recovery state comprehension does not depend on color, animation, icon shape, chart position, or timeline position alone.
  - [ ] Validate text equivalents for charts, matrices, timelines, score bars, graph summaries, JSON/schema views, and status badges.
  - [ ] Validate 44x44px touch targets at phone/tablet widths and zoom or reflow behavior for trust-critical content where local FrontComposer/Playwright tooling supports it.
  - [ ] When browser/tooling support is absent for forced-colors, reduced-motion, zoom, reflow, or screen-reader automation, record the exact unsupported check as a gap and pair it with manual evidence or a deferred release decision; do not mark it as passed by omission.
  - [ ] Include at least one documented screen-reader pass or equivalent manual accessibility checklist for the trust workflow selected by the implementation team. The record must include screen reader or checklist method, browser, OS, tester/date, workflow script, result, defects, severity, owner, waiver state, and release disposition.
  - [ ] Track any unresolved human accessibility defects before release; do not mark validation complete on automated axe pass alone when manual checks fail.

- [ ] Task 5 - Add sanitization and privacy assertions for accessibility surfaces (AC: 6)
  - [ ] Assert visible text, accessible names, tooltips, live announcements, copied text, exported snippets, diagnostics, logs, snapshots, and axe artifacts do not expose secrets, bearer tokens, raw payloads, serialized packets, tenant-sensitive diagnostics, local absolute paths, restricted source details, provider internals, stack traces, or unsanitized exception text.
  - [ ] Reuse Story 2.7 and Epic 17 canonical fixtures for happy, degraded, unauthorized, redacted, omitted/compressed, stale, invalid/schema-mismatch, cross-tenant, and missing-source packets.
  - [ ] Include tenant-isolation cases proving accessible labels and copy/export payloads do not disclose whether evidence exists outside the current authorization scope.
  - [ ] Treat accessibility labels and diagnostics as security surfaces equal to visible UI. Do not build them from raw JSON, exception text, local paths, DOM text reconstruction, or backend diagnostic dumps.
  - [ ] Require a sanitization manifest or redaction scan for committed or archived axe summaries, screenshots, traces, copied/exported text, diagnostics, and manual accessibility evidence.
  - [ ] Seed redaction canaries for bearer tokens, tenant identifiers, restricted source labels, raw packet fragments, local absolute paths, provider internals, stack traces, and cross-tenant hints; fail validation if any canary appears in a user-visible, accessibility, copy/export, diagnostic, or artifact surface.

- [ ] Task 6 - Produce validation evidence and release guardrails (AC: 1-6)
  - [ ] Add an AC-to-evidence matrix listing each surface, viewport width/height, browser/project, state fixture, automated check, keyboard path, focus contract, reduced-motion/forced-colors result, screen-reader/manual result, artifact path, sanitization scan result, and defect disposition.
  - [ ] Distinguish bUnit/unit evidence from Playwright evidence and do not count fixture-only validation as product-surface validation.
  - [ ] Include at least one workflow-level evidence row per role/lens, such as inspect packet, open source/axis/graph detail, understand degraded or recovery state, act or dismiss, and return to origin.
  - [ ] Include reproducibility metadata for each evidence row: command, relative working directory, browser/project, fixture family, viewport, culture/localization setting, media emulation, and sanitized artifact location.
  - [ ] Treat any uncovered trust-critical surface, missing selector, zero-node scan, unsanitized artifact, unsupported tooling gap without manual evidence, or unresolved release-blocking manual defect as `blocked` or `deferred` in the matrix rather than silently passing the story.
  - [ ] Store validation artifacts under existing test artifact or Playwright output conventions. Keep artifacts bounded, sanitized, relative-path-safe, and excluded from commits unless the repo already tracks that class of fixture evidence.
  - [ ] Bound Playwright/E2E to smoke paths per runnable surface and required viewport. Keep state matrices, negative permutations, and redaction cases in bUnit/unit tests unless a browser-specific interaction is being verified.
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
- Deferred decisions must not block fixture/specimen validation, but they must be named in the AC-to-evidence matrix when they prevent a full product-surface validation claim.

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
- 2026-05-20: Party-mode review applied story hardening for dependency gates, runnable-surface inventory, WCAG/tooling gaps, selector and axe scan boundaries, manual accessibility evidence, localization, overlay focus behavior, artifact sanitization, and bounded validation evidence.
- 2026-05-20: Advanced elicitation applied story hardening for fail-closed coverage claims, responsive information parity, redaction canaries, tooling-gap disposition, transition evidence, and reproducibility metadata.

## Party-Mode Review

- Date: 2026-05-20T17:34:00+02:00
- Selected story key: `17-5-responsive-and-accessible-web-validation`
- Command/skill invocation used: `/bmad-party-mode 17-5-responsive-and-accessible-web-validation; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), Sally (UX Designer)
- Findings summary:
  - Story 17.5 was directionally valid but too easy to misread as permission to build missing Epic 17 product surfaces because Story 2.7 is still active and Stories 17.1 through 17.4 are not yet `done`.
  - Runnable surface, fixture-only, blocked, and deferred validation states needed to be separated so specimens cannot be counted as full product-surface validation.
  - Automated accessibility scope needed selector/testid anchors, zero-target-node failures, and explicit WCAG 2.2 AA gap handling where local axe helpers cover only earlier WCAG tags.
  - Manual accessibility evidence needed a concrete record shape for workflow, viewport, AT/browser/OS or checklist method, tester/date, defects, severity, owner, waiver state, and release disposition.
  - Responsive and accessibility validation needed stronger coverage for the Story 17.4 lenses, localization and long-string behavior, overlay focus trapping/background inertness/Escape behavior, touch target sizing, zoom/reflow, workflow-level evidence, and sanitized artifact manifests.
- Changes applied:
  - Expanded AC1 to name the Story 17.4 validation surfaces explicitly and avoid introducing a new generic operator console shell.
  - Added `## Party-Mode Hardening Clarifications` covering validation-only scope, dependency gates, surface inventory, fixture-only reporting, FrontComposer boundaries, bounded Playwright scope, WCAG/tooling gaps, and sanitized evidence requirements.
  - Tightened Task 0 with Story 17.1 through 17.4 completion gating and a required surface inventory table.
  - Tightened Tasks 2 through 5 with selector/testid anchors, zero-node axe guards, WCAG 2.2 gap handling, localization expansion checks, overlay focus contracts, touch/zoom validation, manual evidence record shape, and artifact redaction scans.
  - Tightened Task 6 with evidence-matrix dimensions, bUnit-vs-Playwright separation, fixture-only vs product validation separation, workflow-level evidence rows, and bounded E2E scope.
- Findings deferred:
  - Exact release-blocking threshold for manual screen-reader defects remains a product or release decision.
  - Final artifact retention policy for accessibility evidence remains a product, QA, or governance decision.
  - Mobile grid-to-card transformation strategy remains a UX and implementation decision unless already defined upstream.
  - Unsupported browser and assistive-technology matrix beyond the initial validation set remains a product/QA decision.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- Date: 2026-05-20T18:40:55.2815771+02:00
- Selected story key: `17-5-responsive-and-accessible-web-validation`
- Command/skill invocation used: `/bmad-advanced-elicitation 17-5-responsive-and-accessible-web-validation`
- Batch 1 method names: Red Team vs Blue Team; Security Audit Personas; Failure Mode Analysis; Self-Consistency Validation; Tree of Thoughts
- Reshuffled Batch 2 method names: First Principles Analysis; Pre-mortem Analysis; Architecture Decision Records; Challenge from Critical Perspective; Comparative Analysis Matrix
- Findings summary:
  - Story 17.5 already had strong validation scope controls, but it still needed a fail-closed definition of what counts as coverage so empty scans, missing selectors, and fixture-only evidence cannot become implicit product passes.
  - Responsive validation needed information-parity language so compact layouts preserve canonical trust fields across visible text, accessible names, keyboard/touch paths, and copy/export behavior.
  - Artifact sanitization needed seeded canary checks to prove screenshots, traces, axe summaries, diagnostics, manual notes, and copied/exported text cannot leak tenant-sensitive or machine-local content.
  - Tooling and assistive-technology gaps needed explicit disposition so unsupported checks are tracked as manual evidence, deferred decisions, or blockers rather than silently treated as passed.
  - Evidence rows needed reproducibility and transition semantics so settled-state screenshots cannot hide inaccessible focus, announcement, loading, degraded, stale, unauthorized, or dismissed states.
- Changes applied:
  - Added `## Advanced Elicitation Hardening Clarifications`.
  - Tightened Task 0 with explicit validation claim states.
  - Tightened Tasks 1 through 5 with information-parity, obscured-focus, target-node evidence, state-transition, tooling-gap, and redaction-canary requirements.
  - Tightened Task 6 with reproducibility metadata and fail-closed blocked/deferred matrix behavior.
- Findings deferred:
  - Release-blocking thresholds for manual accessibility defects remain a product or release decision.
  - Accessibility artifact retention policy remains a product, QA, or governance decision.
  - Mobile grid transformation strategy remains a UX and implementation decision unless already decided upstream.
  - Unsupported browser and assistive-technology support beyond the initial validation set remains a product/QA decision.
  - New component semantics or FrontComposer framework behavior remain out of scope unless upstream architecture or product artifacts define them.
- Final recommendation: ready-for-dev

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.
