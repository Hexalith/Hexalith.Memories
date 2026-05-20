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

## Party-Mode Hardening Clarifications

- Contract consumption is read-only: Story 17.1 consumes the canonical `Contracts.V1` Evidence Packet from Story 2.7 and may not add, rename, derive, or reinterpret web-only confidence, freshness, evidence-health, source, retrieval-axis, graph, omitted-detail, scope, or recovery semantics.
- Evidence Cockpit is a presentation/composition slice. Do not add or change backend calls, retrieval orchestration, ranking/scoring rules, graph summarization, MCP behavior, CLI semantics, contract records, or submodule behavior in this story.
- All Trust Strip, Scope Header, Source Citation Stack, Retrieval Axis Breakdown, and Graph Path Summary data must come from canonical Story 2.7-aligned packet fixtures or a named adapter that preserves packet semantics without changing meaning.
- Missing, empty, unknown, unavailable, degraded, unauthorized, redacted, and token-budget-compressed packet values must render as explicit unavailable/degraded/redacted states. Do not infer tenant, case, confidence, citation, retrieval, or graph values from local paths, environment data, secrets, diagnostics, or UI state.
- Tenant and case scope must render before result/evidence content in visual order and DOM order for complete, empty, loading, error, partial, degraded, unauthorized, and compressed states.
- User-facing labels for trust, scope, retrieval axis, graph path, freshness, evidence health, and empty/unavailable states should follow existing FrontComposer localization/resource patterns where available instead of embedding display copy in component logic.
- Validation must use stable roles, accessible labels, or `data-testid` selectors. Do not rely on CSS class selectors, hover-only behavior, brittle visual text, or arbitrary sleeps for Playwright or accessibility checks.
- Public evidence UI, copied text, diagnostic panels, accessibility labels, test snapshots, and trace artifacts must not expose secrets, bearer tokens, local absolute paths, raw payloads, tenant-sensitive diagnostics, or machine-specific filesystem references.

## Advanced Elicitation Hardening Clarifications

- Treat Story 2.7 as the single contract authority. If its Evidence Packet implementation is not complete at dev time, Story 17.1 may use canonical fixtures only for component work and must not edit active Story 2.7 contract, CLI, MCP, or mapper files to unblock the web slice.
- Evidence Packet binding must be field-by-field and explicit. Adapters may normalize presentation shape for FrontComposer, but each displayed trust, scope, source, axis, graph, omitted-detail, recovery, and token-budget value must trace back to a named contract field or render as unavailable.
- When multiple safety or state signals are present, the UI must surface the most restrictive scope/safety condition first in display order without mutating packet values: unauthorized/out-of-scope, schema mismatch or missing source, redacted or compressed detail, degraded or partial evidence, then supported/current/complete evidence.
- Source, axis, and graph inspectors must preserve contract ordering unless the packet exposes an explicit chronological or ranking order. If no ordering signal exists, display the packet order and mark the order basis as unavailable instead of sorting by UI-local heuristics.
- Copy, export, source preview, graph context, and MCP payload inspection must share the same redaction and tenant/case scope checks as the visible UI. Do not reconstruct those outputs from DOM text, diagnostics, browser state, local paths, or raw payload dumps.
- Accessibility and responsive validation must cover transition states as well as settled states: loading to complete, complete to degraded, unauthorized, redacted, compressed, no-source, and graph-backend-unavailable paths all need stable focus, labels, and no hidden trust state.

## Tasks / Subtasks

- [ ] Task 0 - Preflight contract and submodule readiness (AC: 1-4)
  - [ ] Confirm Story 2.7 has landed the canonical `Contracts.V1` Evidence Packet record set or explicitly gate this story until it does. Do not invent a parallel web-only packet shape.
  - [ ] Treat any in-progress Story 2.7 changes as prerequisite context only. Do not patch the active contract story, its source files, or its tests as part of this web story unless Story 17.1 is explicitly re-scoped.
  - [ ] Read `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md`, `src/Hexalith.Memories.Contracts/V1/SearchResult.cs`, `HybridSearchResult.cs`, `SearchExplanation.cs`, `TraversalResult.cs`, and `OmittedReason.cs` before designing UI bindings.
  - [ ] Read `Hexalith.FrontComposer/_bmad-output/project-context.md`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell`, and `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Testing` before adding components.
  - [ ] Verify the current Fluent UI Blazor package in `Hexalith.FrontComposer/Directory.Packages.props` before using examples. The local package is `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.2-26098.1`; the available MCP documentation was for `5.0.0.26098`, so local code/tests are the stronger source when signatures disagree.

- [ ] Task 1 - Define the Evidence Cockpit composition boundary (AC: 1, 4)
  - [ ] Add the smallest FrontComposer-aligned web surface that can render an Evidence Packet for a selected tenant and case. Prefer a contract-driven composition or view model adapter over page-specific JSON parsing.
  - [ ] Keep tenant and case scope before the query/result content in the visual and DOM order.
  - [ ] Preserve the shared Evidence Packet state grammar: confidence (`supported`, `partial`, `disputed`, `insufficient`), freshness (`current`, `aging`, `stale`, `unknown`), evidence health (`complete`, `degraded`, `missing source`, `schema mismatch`), and scope (`verified`, `inferred`, `cross-case`, `unauthorized`, `out-of-scope`).
  - [ ] Define an explicit packet-to-view mapping table in code or tests so every rendered trust and evidence value has a named contract source, including the unavailable fallback when the source field is absent.
  - [ ] Apply restrictive-state display precedence without changing packet semantics: scope/safety failures first, then schema/source degradation, then redaction/compression, then partial evidence, then supported evidence.
  - [ ] Do not add a new search backend, MCP tool, CLI output shape, or contract semantics in this story. If the existing contract is insufficient, record the gap as a dependency on Story 2.7 or a deferred decision.
  - [ ] Keep the cockpit shell presentation-only. Filtering, ranking, evidence mutation, export payload design, and drill-down workflows beyond safely opening already-present packet details are deferred unless the packet already exposes enough data and FrontComposer already has the command primitive.

- [ ] Task 2 - Implement Trust Strip and Scope Header components (AC: 1, 2)
  - [ ] Render tenant id/name, case id/name, scope status, confidence state, freshness state, source count, evidence health, and token-budget indicator from the Evidence Packet contract.
  - [ ] Use Fluent UI/FrontComposer status primitives such as badges, inline messages, layout stacks, menus, or command surfaces before creating custom controls.
  - [ ] Ensure every state has visible text and an accessible name. Color, icon, or badge appearance alone is not sufficient.
  - [ ] Add compact wrapping behavior so the strip remains before the answer at mobile widths instead of disappearing into an overflow-only control.
  - [ ] Cover complete, empty, loading, error, partial, degraded, unauthorized, redacted, and token-budget-compressed state rendering without moving scope below result content.

- [ ] Task 3 - Implement source, axis, and graph inspection summaries (AC: 3, 4)
  - [ ] Source Citation Stack exposes source type, origin identifier, snippet or summary, freshness, confidence/metadata origin when available, and keyboard-openable preview behavior.
  - [ ] Retrieval Axis Breakdown exposes retrieval axes used, normalized score or contribution, ranking reason, unavailable/degraded markers, and the `SearchExplanation.Caveat` meaning where present.
  - [ ] Graph Path Summary exposes relationship path, edge type, confidence, gap markers, chronological ordering, depth, and graph-backend degraded state when available.
  - [ ] Keep first view compact: trust essentials first, then expandable source/detail panels. Avoid dashboard sprawl and decorative cards that hide the evidence workflow.
  - [ ] Render absent sources, absent axes, absent graph paths, unknown freshness, unavailable backend detail, and redacted source content as explicit unavailable/degraded/redacted states, not empty certainty.
  - [ ] Preserve source, axis, and graph ordering from the packet unless the contract provides an explicit ranking or chronological field. Do not introduce UI-local sorting that can change evidence meaning.

- [ ] Task 4 - Wire FrontComposer command/navigation behavior (AC: 1, 4)
  - [ ] Use FrontComposer tenant/user render context and shell conventions for scope-aware rendering; do not store tenant/case state in singleton/static UI services.
  - [ ] Preserve context when navigating from the Evidence Packet to a source, graph path, activity item, or agent packet.
  - [ ] Provide command actions for inspect source, open graph context, export packet, retry/refine query, and inspect MCP payload only when the packet exposes enough data to perform the action safely.
  - [ ] Apply the same redaction, unavailable-state, tenant, and case checks to copy/export/inspect commands as to visible components. Do not build command payloads from rendered DOM text or diagnostic dumps.
  - [ ] Confirmation or scope-expanding actions must name the tenant, case, target object, and consequence before proceeding.

- [ ] Task 5 - Add component and accessibility tests (AC: 1-4)
  - [ ] Add bUnit coverage using `Hexalith.FrontComposer.Testing` or the existing `BunitContext` + `AddFluentUIComponents()` pattern.
  - [ ] Render canonical Story 2.7-aligned Evidence Packet fixtures in component tests, including complete, empty, partial, degraded, multi-source, tenant/case mismatch, unauthorized, redacted, and token-budget-compressed packets.
  - [ ] Add mapping tests that fail when a rendered field has no named Evidence Packet source or when a contract field used by the Trust Strip, Scope Header, Source Citation Stack, Retrieval Axis Breakdown, or Graph Path Summary is silently dropped.
  - [ ] Test Trust Strip state labels and accessible names for supported, partial/degraded, unauthorized, and token-budget-compressed packets.
  - [ ] Test restrictive-state precedence, packet ordering preservation, and unavailable-order messaging for source, axis, and graph summaries.
  - [ ] Test source/axis/graph expansion keeps keyboard-reachable controls and does not depend on hover-only behavior.
  - [ ] Test tenant/case scope appears before result content in markup order.
  - [ ] Add negative tests that restricted source details, raw payloads, bearer tokens, tenant-sensitive diagnostics, and local absolute paths do not render in accessible labels, copied text, logs, or diagnostic panels.
  - [ ] If copy, export, graph context, or MCP inspection commands are visible, add negative tests that their payloads follow the same redaction and scope rules as the visible Evidence Cockpit.
  - [ ] Use role, accessible-label, and `data-testid` selectors for UI tests. Do not use CSS class selectors, arbitrary sleeps, or selectors coupled only to visual text.

- [ ] Task 6 - Validate responsive and visual behavior (AC: 1-4)
  - [ ] Run focused unit/bUnit tests for any Memories web project and changed FrontComposer component tests.
  - [ ] If a runnable web surface is added, run Playwright or equivalent browser checks at 360px, 768px, 1024px, and 1440px. Capture evidence that scope, confidence, freshness, source count, evidence health, and recovery remain reachable.
  - [ ] Run automated accessibility checks where the repo already supports them. For FrontComposer E2E, use the existing `tests/e2e` axe helper pattern.
  - [ ] Validate keyboard-only operation, focus order, focus visibility, focus return from dialogs/drawers, screen-reader names, touch target sizing, forced-colors/high-contrast behavior, and no text overlap at narrow mobile, tablet, desktop, and wide desktop widths.
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
- 2026-05-20: Party-mode review applied story hardening for contract consumption boundaries, explicit unavailable/redacted states, accessibility validation, stable selectors, and scope-limited web composition.

## Party-Mode Review

- Date: 2026-05-20T11:31:23.1043511+02:00
- Selected story key: `17-1-evidence-cockpit-and-trust-components`
- Command/skill invocation used: `/bmad-party-mode 17-1-evidence-cockpit-and-trust-components; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - Story 17.1 was directionally valid but needed sharper boundaries so the future web Evidence Cockpit consumes Story 2.7 `Contracts.V1` Evidence Packet semantics without inventing web-only trust, citation, retrieval-axis, graph, omitted-detail, scope, or recovery meanings.
  - The Evidence Cockpit scope risk was UI expansion beyond reusable presentation/composition components into filtering, ranking, export, mutation, backend, MCP, CLI, or contract changes.
  - Trust Strip, Scope Header, Source Citation Stack, Retrieval Axis Breakdown, and Graph Path Summary needed explicit behavior for empty, missing, unknown, unavailable, degraded, unauthorized, redacted, and compressed packet values.
  - Accessibility and responsive requirements needed executable validation language for keyboard order, focus behavior, touch targets, screen-reader names, forced-colors/high-contrast, stable selectors, and no text overlap.
  - Tests needed canonical Story 2.7-aligned fixtures and negative assertions for secrets, local paths, raw payloads, tenant-sensitive diagnostics, and brittle UI selectors.
- Changes applied:
  - Added `## Party-Mode Hardening Clarifications`.
  - Tightened Task 1 with presentation-only cockpit boundaries and deferred filtering/ranking/mutation/export semantics.
  - Tightened Task 2 and Task 3 with explicit complete, empty, loading, error, partial, degraded, unauthorized, redacted, compressed, absent-source, absent-axis, absent-graph, and unknown-value rendering expectations.
  - Tightened Task 5 with canonical Evidence Packet fixture coverage, selector stability, and non-leakage test expectations.
  - Tightened Task 6 with keyboard, focus, screen-reader, touch target, forced-colors/high-contrast, responsive, and no-text-overlap validation.
- Findings deferred:
  - Final visual density and information hierarchy remain implementation decisions within the clarified component boundaries.
  - New trust scoring, retrieval weighting, graph summarization, backend behavior, MCP behavior, CLI behavior, and contract semantics remain out of scope for Story 17.1.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- Date: 2026-05-20T12:20:27.1576382+02:00
- Selected story key: `17-1-evidence-cockpit-and-trust-components`
- Command/skill invocation used: `/bmad-advanced-elicitation 17-1-evidence-cockpit-and-trust-components`
- Batch 1 method names: Red Team vs Blue Team, Security Audit Personas, Failure Mode Analysis, Self-Consistency Validation, Tree of Thoughts
- Reshuffled Batch 2 method names: First Principles Analysis, Pre-mortem Analysis, Architecture Decision Records, Challenge from Critical Perspective, Comparative Analysis Matrix
- Findings summary:
  - Story 17.1 needed a clearer guardrail for the active Story 2.7 dependency so future web work does not mutate contract, CLI, MCP, mapper, or test files owned by the in-progress Evidence Packet story.
  - The web adapter boundary needed field-level traceability so trust, scope, source, axis, graph, omitted-detail, recovery, and token-budget values are either mapped from named contract fields or explicitly unavailable.
  - Multi-signal packet states needed deterministic display precedence that surfaces restrictive safety and scope conditions first without creating new contract semantics.
  - Source, axis, graph, copy, export, and inspector paths needed stronger non-leakage and ordering constraints to prevent UI-local sorting, DOM reconstruction, or diagnostics from changing or exposing evidence.
  - Accessibility and responsive validation needed to include transition and degraded states, not only settled successful packet rendering.
- Changes applied:
  - Added `## Advanced Elicitation Hardening Clarifications`.
  - Tightened Task 0 with the Story 2.7 active-contract ownership boundary.
  - Tightened Task 1 with explicit packet-to-view mapping and restrictive-state display precedence.
  - Tightened Task 3 with packet ordering preservation for source, axis, and graph summaries.
  - Tightened Task 4 with redaction and scope parity for copy/export/inspect command payloads.
  - Tightened Task 5 with mapping, precedence, ordering, and command-payload negative tests.
- Findings deferred:
  - Exact component names, adapter type names, and visual layout remain implementation decisions inside FrontComposer conventions.
  - Contract changes, new Evidence Packet fields, new retrieval ranking, graph summarization, CLI/MCP output changes, and Story 2.7 source/test edits remain out of scope for Story 17.1.
- Final recommendation: ready-for-dev

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.
