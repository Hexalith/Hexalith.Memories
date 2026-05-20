# Story 17.1: Evidence Cockpit and Trust Components

Status: ready-for-dev

## Story

As a developer or team lead,
I want a FrontComposer/Fluent UI Evidence Cockpit with Evidence Packet, Trust Strip, Scope Header, source, axis, and graph summaries,
so that I can verify answers, sources, retrieval reasons, scope, and graph context in one inspectable web workflow.

## Acceptance Criteria

1. Given the web Evidence Cockpit is opened for a tenant and case, when a search or briefing response is displayed, then the page composes the shared Evidence Packet contract without inventing new confidence, state, omitted-detail, source, graph, or recovery semantics, and tenant and case scope are visible before the query or briefing content.
2. Given an Evidence Packet is displayed, when the Trust Strip renders, then it shows tenant, case, confidence state, freshness state, source count, evidence health, and token-budget indicator when applicable, and each state has a text label and accessible name, not color alone.
3. Given a result has sources, axis scores, or graph context, when the user expands evidence details, then Source Citation Stack, Retrieval Axis Breakdown, and Graph Path Summary components expose source type, origin identifier, freshness, score normalization, ranking reason, edge type, confidence, gap markers, and chronological ordering as available from the contract.
4. Given the UI uses FrontComposer and Fluent UI Blazor, when controls, panels, tabs, grids, or menus are needed, then existing primitives are used before custom controls, and custom Memories components remain contract-aware and tenant-aware.

## Tasks / Subtasks

- [ ] Task 0 - Preflight contract and submodule readiness (AC: 1-4)
  - [ ] Confirm Story 2.7 has landed the canonical `Contracts.V1` Evidence Packet record set or explicitly gate this story until it does. Do not invent a parallel web-only packet shape.
  - [ ] Read `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md`, `src/Hexalith.Memories.Contracts/V1/SearchResult.cs`, `HybridSearchResult.cs`, `SearchExplanation.cs`, `TraversalResult.cs`, and `OmittedReason.cs` before designing UI bindings.
  - [ ] Read `Hexalith.FrontComposer/_bmad-output/project-context.md`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell`, and `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Testing` before adding components.
  - [ ] Verify the current Fluent UI Blazor package in `Hexalith.FrontComposer/Directory.Packages.props` before using examples. The local package is `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.2-26098.1`; the available MCP documentation was for `5.0.0.26098`, so local code/tests are the stronger source when signatures disagree.

- [ ] Task 1 - Define the Evidence Cockpit composition boundary (AC: 1, 4)
  - [ ] Add the smallest FrontComposer-aligned web surface that can render an Evidence Packet for a selected tenant and case. Prefer a contract-driven composition or view model adapter over page-specific JSON parsing.
  - [ ] Keep tenant and case scope before the query/result content in the visual and DOM order.
  - [ ] Preserve the shared Evidence Packet state grammar: confidence (`supported`, `partial`, `disputed`, `insufficient`), freshness (`current`, `aging`, `stale`, `unknown`), evidence health (`complete`, `degraded`, `missing source`, `schema mismatch`), and scope (`verified`, `inferred`, `cross-case`, `unauthorized`, `out-of-scope`).
  - [ ] Do not add a new search backend, MCP tool, CLI output shape, or contract semantics in this story. If the existing contract is insufficient, record the gap as a dependency on Story 2.7 or a deferred decision.

- [ ] Task 2 - Implement Trust Strip and Scope Header components (AC: 1, 2)
  - [ ] Render tenant id/name, case id/name, scope status, confidence state, freshness state, source count, evidence health, and token-budget indicator from the Evidence Packet contract.
  - [ ] Use Fluent UI/FrontComposer status primitives such as badges, inline messages, layout stacks, menus, or command surfaces before creating custom controls.
  - [ ] Ensure every state has visible text and an accessible name. Color, icon, or badge appearance alone is not sufficient.
  - [ ] Add compact wrapping behavior so the strip remains before the answer at mobile widths instead of disappearing into an overflow-only control.

- [ ] Task 3 - Implement source, axis, and graph inspection summaries (AC: 3, 4)
  - [ ] Source Citation Stack exposes source type, origin identifier, snippet or summary, freshness, confidence/metadata origin when available, and keyboard-openable preview behavior.
  - [ ] Retrieval Axis Breakdown exposes retrieval axes used, normalized score or contribution, ranking reason, unavailable/degraded markers, and the `SearchExplanation.Caveat` meaning where present.
  - [ ] Graph Path Summary exposes relationship path, edge type, confidence, gap markers, chronological ordering, depth, and graph-backend degraded state when available.
  - [ ] Keep first view compact: trust essentials first, then expandable source/detail panels. Avoid dashboard sprawl and decorative cards that hide the evidence workflow.

- [ ] Task 4 - Wire FrontComposer command/navigation behavior (AC: 1, 4)
  - [ ] Use FrontComposer tenant/user render context and shell conventions for scope-aware rendering; do not store tenant/case state in singleton/static UI services.
  - [ ] Preserve context when navigating from the Evidence Packet to a source, graph path, activity item, or agent packet.
  - [ ] Provide command actions for inspect source, open graph context, export packet, retry/refine query, and inspect MCP payload only when the packet exposes enough data to perform the action safely.
  - [ ] Confirmation or scope-expanding actions must name the tenant, case, target object, and consequence before proceeding.

- [ ] Task 5 - Add component and accessibility tests (AC: 1-4)
  - [ ] Add bUnit coverage using `Hexalith.FrontComposer.Testing` or the existing `BunitContext` + `AddFluentUIComponents()` pattern.
  - [ ] Test Trust Strip state labels and accessible names for supported, partial/degraded, unauthorized, and token-budget-compressed packets.
  - [ ] Test source/axis/graph expansion keeps keyboard-reachable controls and does not depend on hover-only behavior.
  - [ ] Test tenant/case scope appears before result content in markup order.
  - [ ] Add negative tests that restricted source details, raw payloads, bearer tokens, tenant-sensitive diagnostics, and local absolute paths do not render in accessible labels, copied text, logs, or diagnostic panels.

- [ ] Task 6 - Validate responsive and visual behavior (AC: 1-4)
  - [ ] Run focused unit/bUnit tests for any Memories web project and changed FrontComposer component tests.
  - [ ] If a runnable web surface is added, run Playwright or equivalent browser checks at 360px, 768px, 1024px, and 1440px. Capture evidence that scope, confidence, freshness, source count, evidence health, and recovery remain reachable.
  - [ ] Run automated accessibility checks where the repo already supports them. For FrontComposer E2E, use the existing `tests/e2e` axe helper pattern.
  - [ ] Run `git diff --check`.

## Dev Notes

### Current Implementation State

- Epic 17 is explicitly future web UI scope. This story is valid when web UI is selected, but it must not weaken the CLI/MCP Evidence Packet contract or pull unrelated MVP work forward.
- Story 2.7 owns the shared Evidence Packet grammar in `Contracts.V1`. Story 17.1 consumes that grammar through FrontComposer/Fluent UI composition. The web layer must not define separate confidence, degraded, omitted-detail, source, graph, or recovery vocabularies.
- Current lower-level retrieval contracts already expose many source inputs: `SearchResult`, `HybridSearchResult`, `ScoredResult`, `FusedScoredResult`, `SearchExplanation`, `TraversalResult`, and `OmittedReason`. Use them only through the Evidence Packet contract or a clearly named adapter that preserves contract semantics.
- `Hexalith.FrontComposer` is a root-level submodule. Treat its existing contracts, shell components, source generators, and testing helpers as the local implementation foundation. Do not initialize nested submodules or casually mutate unrelated submodule history.
- FrontComposer currently provides projection roles (`ActionQueue`, `StatusOverview`, `DetailRecord`, `Timeline`, `Dashboard`), semantic badge slots, tenant/user render context, command palette/state, data-grid navigation, projection templates, view override hosts, diagnostics, and `Hexalith.FrontComposer.Testing` bUnit utilities.
- FrontComposer test patterns register `AddFluentUIComponents()` and localization in bUnit contexts when Fluent primitives or diagnostics render. Prefer that pattern for component tests.

### Component Boundaries

- Evidence Cockpit is the page/workspace composition. It should arrange scope/query/result/evidence/recovery without becoming a new design system.
- Trust Strip and Scope Header are trust-critical. They must render before answer/result content and keep labels available in compact layouts.
- Source Citation Stack, Retrieval Axis Breakdown, and Graph Path Summary are expandable inspection components. They should render only data the contract actually provides and should expose unknown/unavailable state rather than fabricating certainty.
- Use Fluent UI primitives for mechanics: buttons, menus, tabs, data grids, badges, banners, dialogs, drawers, progress indicators, tooltips, inputs, layout, and focus behavior.
- Use FrontComposer conventions for composition: typed descriptors, tenant-aware context, command lifecycle visibility, bounded diagnostics, Fluxor-compatible state, render-mode-safe components, and component test helpers.

### Accessibility and UX Guardrails

- Trust state is product correctness. Confidence, freshness, evidence health, scope status, degraded backends, and token-budget compression require text labels and accessible names.
- Keyboard support must cover the trust loop: select tenant, select case, submit query, inspect evidence, expand sources, inspect axis detail, open graph context, follow recovery action, and return.
- Drawers, dialogs, source previews, graph detail panels, and MCP inspectors must move focus in and return focus to the invoking control when closed.
- Do not rely on hover-only interactions. Every source preview, graph detail, recovery action, tooltip-critical command, and status explanation must work by keyboard and touch.
- Support forced-colors/high-contrast through Fluent UI tokens and semantic labels. Do not encode state only through custom color choices.
- Keep the interface dense and operational. Avoid marketing-style panels, large decorative empty states, or dashboards that bury source/evidence/recovery behind visual decoration.

### Testing Notes

- Component tests should use xUnit, Shouldly, bUnit, and NSubstitute where relevant, matching FrontComposer and Memories conventions.
- If the implementation changes FrontComposer submodule files, run tests from the submodule root and keep those changes intentional and separately reviewable.
- If a Memories web project is introduced, keep package versions centralized and avoid adding `Version` attributes to `.csproj` package references.
- Browser/accessibility validation should use existing Playwright and axe patterns where available. For FrontComposer, `Hexalith.FrontComposer/tests/e2e` already documents viewport, fixture, and `@axe-core/playwright` conventions.

### Dependencies and Non-Goals

- Dependency: Story 2.7 Evidence Packet Contract Mapping must provide the contract semantics consumed here. If it is not implemented at dev time, this story should pause or use test fixtures only until the contract is available.
- Non-goal: no new retrieval algorithm, ingestion workflow, MCP server behavior, CLI output contract, benchmark logic, or operator health matrix.
- Non-goal: no broad FrontComposer framework redesign. Add Memories-specific compositions only where existing FrontComposer primitives do not already cover the need.
- Non-goal: no nested submodule initialization or recursive submodule update.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 17 and Story 17.1 acceptance criteria.
- `_bmad-output/planning-artifacts/ux-design-specification.md` - Evidence Packet, Trust Strip, Scope Header, Source Citation Stack, Retrieval Axis Breakdown, Graph Path Summary, responsive, and accessibility requirements.
- `_bmad-output/planning-artifacts/architecture.md` - Evidence Packet Contract ownership and lower-level retrieval contract boundary.
- `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md` - prerequisite Evidence Packet contract story and current contract mapping guidance.
- `_bmad-output/project-context.md` - Memories project rules for .NET, contracts, tests, warnings-as-errors, and submodules.
- `Hexalith.FrontComposer/_bmad-output/project-context.md` - FrontComposer-specific implementation rules.
- `Hexalith.FrontComposer/tests/README.md` - bUnit, Playwright, axe, and E2E testing conventions.
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Testing/README.md` - component test host utilities.
- `Hexalith.FrontComposer/Directory.Packages.props` - local Fluent UI Blazor package version.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Created from sprint status backlog item `17-1-evidence-cockpit-and-trust-components`.
- Loaded preflight JSON, sprint status, story lessons, Epic 17 requirements, UX component/accessibility sections, architecture Evidence Packet section, Story 2.7 artifact, current lower-level search/traversal contracts, FrontComposer package/test/component context, and Fluent UI Blazor version compatibility warning.
- No product code implementation performed in this create-story workflow.

### Completion Notes List

- Ready-for-dev story created on 2026-05-20.
- Scope is limited to future web Evidence Cockpit composition and trust components over the shared Evidence Packet contract.
- Story explicitly records the Story 2.7 dependency and local Fluent UI Blazor version mismatch with the MCP documentation source.

### File List

- `_bmad-output/implementation-artifacts/17-1-evidence-cockpit-and-trust-components.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/process-notes/predev-hardening-runs.log`

## Change Log

- 2026-05-20: Created ready-for-dev story artifact for Evidence Cockpit and Trust Components.

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.
