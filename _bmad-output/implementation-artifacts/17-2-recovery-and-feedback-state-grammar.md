# Story 17.2: Recovery and Feedback State Grammar

Status: ready-for-dev

## Story

As a developer or operator,
I want weak, empty, stale, degraded, unauthorized, compressed, and conflicting evidence states to show clear recovery guidance,
so that I can decide the next safe action without leaving the current workflow.

## Acceptance Criteria

1. Given an Evidence Packet is empty, weak, stale, degraded, unauthorized, compressed, or disputed, when the state is displayed, then the UI shows a clear state title, explanation, diagnostic clue, severity, affected capability, and one safest recovery action, and optional secondary actions are available without hiding the primary recovery path.
2. Given no-result or low-evidence states occur, when the Recovery Action Panel renders, then it distinguishes no match, not ingested yet, wrong case, inaccessible tenant/case, stale memory, degraded backend, graph gap, and insufficient evidence where the response data allows.
3. Given sources, freshness, scores, graph context, or backend health disagree, when evidence is presented, then the conflict is visible using the shared evidence state grammar rather than converted into a confident-looking answer.
4. Given feedback appears in the web UI, when users inspect it with keyboard or assistive technology, then status labels are readable, focusable recovery actions are reachable, and color is never the only signal.

## Tasks / Subtasks

- [ ] Task 0 - Confirm dependency and local UI foundation (AC: 1-4)
  - [ ] Confirm Story 2.7 has landed the canonical `Contracts.V1` Evidence Packet record set. If it has not, pause implementation or use fixtures only; do not create a web-only recovery/state vocabulary.
  - [ ] Read `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md` and the implemented `EvidencePacket`, `EvidencePacketState`, `EvidencePacketOmittedDetails`, `EvidencePacketExpansionHandle`, and `EvidencePacketRecoveryAction` contracts before binding UI.
  - [ ] Read Story 17.1 before implementation and reuse its Evidence Cockpit, Trust Strip, Scope Header, FrontComposer, Fluent UI, accessibility, and responsive guardrails.
  - [ ] Verify the local Fluent UI Blazor package in `Hexalith.FrontComposer/Directory.Packages.props` before copying examples. The local package is `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.2-26098.1`; the available MCP documentation is for `5.0.0.26098`, so local code/tests are authoritative when signatures differ.

- [ ] Task 1 - Define the recovery state mapping boundary (AC: 1-3)
  - [ ] Add or extend the smallest Memories web adapter/view-model that maps Evidence Packet state and recovery fields to display state. Keep the mapping pure and testable.
  - [ ] Preserve the shared state grammar: confidence (`supported`, `partial`, `disputed`, `insufficient`), freshness (`current`, `aging`, `stale`, `unknown`), evidence health (`complete`, `degraded`, `missing source`, `schema mismatch`), and scope (`verified`, `inferred`, `cross-case`, `unauthorized`, `out-of-scope`).
  - [ ] Represent the state dimensions required by this story: empty, weak, stale, degraded, unauthorized, compressed, disputed/conflicting, no match, not ingested yet, wrong case, inaccessible tenant/case, graph gap, and insufficient evidence.
  - [ ] Do not infer precision the contract does not provide. If the packet cannot distinguish two causes, render an explicit unknown or insufficient-evidence state with a safe recovery action instead of guessing.

- [ ] Task 2 - Implement Recovery Action Panel/Footer behavior (AC: 1, 2, 4)
  - [ ] Render a Recovery Action Panel or packet footer close to the affected Evidence Packet, not only in a global toast or notification region.
  - [ ] Show state title, short explanation, diagnostic clue, severity, affected capability, and one safest primary action before secondary actions.
  - [ ] Use FrontComposer and Fluent UI primitives first: `FluentMessageBar`, `FluentBadge`, buttons, menus, panels/drawers, dialogs, tooltips, inline messages, and existing shell feedback components where they fit.
  - [ ] Ensure recovery commands name their tenant, case, target object, and consequence when they can broaden scope, retry ingestion, request permission, repair consistency, or expose diagnostics.
  - [ ] Do not execute unsafe recovery work directly from the component. Route through existing command, navigation, or handler conventions so lifecycle, authorization, diagnostics, and tenant context remain visible.

- [ ] Task 3 - Make conflict and compression states inspectable (AC: 1-4)
  - [ ] Surface conflicts between sources, freshness, retrieval scores, graph support, and backend health as conflicting/disputed evidence, not as a confident answer with hidden caveats.
  - [ ] Show compressed or token-budget-limited packets with omitted detail names and deterministic expansion handles or equivalent expansion guidance from the contract.
  - [ ] Keep primary trust labels visible in compact layouts: confidence, freshness, evidence health, scope, source count, affected capability, and recovery action.
  - [ ] Render unavailable axes, missing sources, schema mismatch, graph gaps, and degraded backends as explicit states with labels and diagnostic clues.

- [ ] Task 4 - Accessibility, localization, and sanitization guardrails (AC: 1-4)
  - [ ] Every state must have visible text and an accessible name. Color, badge appearance, icon, animation, or placement alone is not sufficient.
  - [ ] Recovery actions must be reachable by keyboard and touch, have deterministic focus order, and return focus to the invoking control after dialogs, drawers, or panels close.
  - [ ] Use `role="status"` / `aria-live="polite"` for non-blocking updates and `role="alert"` / assertive announcement only for blocking or safety-critical states.
  - [ ] Keep strings localizable through existing FrontComposer resource patterns when the component is added to FrontComposer shell code.
  - [ ] Do not render secrets, bearer tokens, raw payloads, tenant-sensitive diagnostics, local absolute paths, restricted source details, or unsanitized exception text in visible labels, accessible labels, copied text, diagnostics, logs, or snapshots.

- [ ] Task 5 - Add focused component, mapping, and accessibility tests (AC: 1-4)
  - [ ] Add bUnit coverage using `Hexalith.FrontComposer.Testing`, `FrontComposerTestBase`, or the existing `BunitContext` pattern as appropriate.
  - [ ] Test recovery mapping for empty, no match, not ingested yet, wrong case, inaccessible tenant/case, stale, degraded backend, graph gap, insufficient evidence, unauthorized, compressed, and disputed/conflicting packets.
  - [ ] Test that each state renders title, explanation, diagnostic clue, severity, affected capability, primary recovery action, and optional secondary actions in the intended order.
  - [ ] Test keyboard-reachable primary and secondary actions, panel/dialog focus return, visible labels, and accessible names.
  - [ ] Add negative tests proving restricted details, local paths, raw payloads, bearer tokens, tenant-sensitive diagnostics, and unsanitized exception text do not render in markup, accessible labels, copied text, logs, or snapshots.

- [ ] Task 6 - Validate responsive and visual behavior (AC: 1-4)
  - [ ] Run focused unit/bUnit tests for changed Memories web or FrontComposer component projects.
  - [ ] If a runnable web surface is added, run Playwright or equivalent browser checks at 360px, 768px, 1024px, and 1440px and capture evidence that state labels and recovery actions remain reachable.
  - [ ] Run automated accessibility checks where the repo already supports them. For FrontComposer E2E, use the existing `tests/e2e` axe helper pattern.
  - [ ] Run `git diff --check`.

## Dev Notes

### Current Implementation State

- Epic 17 is explicitly future web UI scope. This story is valid when web UI work is selected, but it must not weaken CLI/MCP Evidence Packet behavior or pull unrelated MVP work into scope.
- Story 17.1 created the first Future Web UI story context for Evidence Cockpit, Trust Strip, Scope Header, source, axis, and graph summaries. Story 17.2 builds on that cockpit by making weak, empty, stale, degraded, unauthorized, compressed, and conflicting states actionable.
- Story 2.7 owns the shared Evidence Packet grammar in `Contracts.V1`. Story 17.2 consumes `state`, `omittedDetails`, `expansionHandles`, and `recovery` semantics through FrontComposer/Fluent UI composition. The web layer must not invent separate recovery codes, confidence labels, degraded-state names, or omitted-detail meanings.
- FrontComposer already contains feedback/status patterns worth reusing: `FcProjectionConnectionStatus` uses `FluentMessageBar` with `role="status"` and `aria-live="polite"`; `FcCustomizationDiagnosticPanel` uses alert semantics for blocking diagnostics; `FcPendingCommandSummary`, destructive confirmation dialogs, filter empty states, status badges, command palette, lifecycle wrapper, and tenant navigation state provide local patterns for bounded feedback, commands, and focusable interaction.
- FrontComposer test patterns include `FrontComposerTestBase`, bUnit shell tests, `AddFluentUIComponents()` patterns, axe-backed Playwright helpers, `data-testid` selectors, and redaction scanners in Pact/governance tests. Reuse those patterns before adding bespoke test infrastructure.

### Recovery State Semantics

- The panel/footer answers four questions: what happened, what it affects, how serious it is, and what to do next.
- The primary recovery action must be the safest available action from the packet. Examples include refine query, inspect source, compare versions, open graph neighborhood, refresh ingestion, verify tenant, request permission, retry agent/tool call, repair consistency, export packet, or escalate.
- Optional secondary actions are allowed, but they must not obscure the primary recovery path. Use menus or secondary button grouping when space is constrained.
- No-result states must distinguish:
  - no match: the search completed but found no supported candidate;
  - not ingested yet: matching knowledge may be pending ingestion/indexing;
  - wrong case: the query likely belongs to another selected case;
  - inaccessible tenant/case: authorization or scope prevents access;
  - stale memory: available sources may be old or superseded;
  - degraded backend: one or more retrieval axes/backends are unavailable;
  - graph gap: graph context is incomplete or missing causal links;
  - insufficient evidence: the system cannot support a confident answer from available data.
- Conflicting evidence must remain visible. Do not smooth contradictory source freshness, lexical/semantic/graph disagreement, missing source details, or backend disagreement into a single confident-looking answer.
- Compressed/token-budget states must name omitted detail groups and expose deterministic expansion handles or equivalent guidance. Do not hide compression as a generic partial state.

### Component Boundaries

- Recovery Action Panel/Footer is part of the Evidence Packet workflow. It belongs near the affected packet or state, not as a detached notification-only feature.
- The component should consume a typed packet or adapter result, not arbitrary JSON or stringly typed diagnostics.
- Use existing FrontComposer tenant/user render context, command lifecycle, navigation, feedback, diagnostics, and shell composition conventions.
- Confirmation or scope-expanding actions must name tenant, case, target object, and consequence before proceeding.
- Keep the interface dense and operational. Avoid dashboard sprawl, marketing-style panels, decorative cards, or empty states that hide the recovery action.

### Accessibility and UX Guardrails

- Trust-critical feedback must be visible in the layout and accessible to assistive technology.
- Use semantic severity, but do not rely on severity color alone. Pair visual treatments with text labels and accessible names.
- Recovery actions must work by keyboard and touch. Do not rely on hover-only tooltips or mouse-only source previews.
- Focus management matters for panels, drawers, dialogs, confirmation flows, and action menus: focus enters predictably and returns to the invoking control when closed.
- Forced-colors/high-contrast and reduced-motion modes must preserve state comprehension. Prefer Fluent UI tokens and semantic components over custom color systems.
- Do not expose secrets, raw payloads, bearer tokens, tenant-sensitive diagnostics, local absolute paths, or restricted source details in visible text, accessible text, copied text, diagnostic panels, logs, or snapshots.

### Testing Notes

- Component tests should use xUnit, Shouldly, bUnit, and NSubstitute where relevant, matching FrontComposer and Memories conventions.
- If implementation changes FrontComposer submodule files, run tests from the submodule root and keep those changes intentional and separately reviewable.
- If implementation introduces a Memories web project, keep package versions centralized and avoid adding `Version` attributes to `.csproj` package references.
- Browser/accessibility validation should use existing Playwright and axe patterns where available. For FrontComposer, `Hexalith.FrontComposer/tests/e2e` documents viewport, fixture, selector, and axe conventions.

### Dependencies and Non-Goals

- Dependency: Story 2.7 Evidence Packet Contract Mapping must provide the contract semantics consumed here. If it is not implemented at dev time, this story should pause or use contract fixtures only until the dependency is available.
- Dependency: Story 17.1 should define or establish the Evidence Cockpit composition boundary this story extends.
- Non-goal: no new retrieval algorithm, ingestion workflow, MCP server behavior, CLI output contract, benchmark logic, tenant authorization model, or operator health matrix.
- Non-goal: no new recovery action semantics outside the Evidence Packet contract. If a needed recovery action is missing, record the contract gap for Story 2.7 or a follow-up.
- Non-goal: no broad FrontComposer framework redesign and no nested submodule initialization or recursive submodule update.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 17 and Story 17.2 acceptance criteria.
- `_bmad-output/planning-artifacts/ux-design-specification.md` - Evidence Packet invariants, recovery footer, state grammar, no-result distinctions, conflict visibility, accessibility, responsive, and future web requirements.
- `_bmad-output/planning-artifacts/architecture.md` - `Contracts.V1`, Evidence Packet ownership, tenant isolation, structured errors, and recovery/degraded-service constraints.
- `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md` - prerequisite Evidence Packet contract and recovery/omitted-detail semantics.
- `_bmad-output/implementation-artifacts/17-1-evidence-cockpit-and-trust-components.md` - prior Epic 17 story and web composition guardrails.
- `_bmad-output/project-context.md` - Memories project rules for .NET, contracts, tests, warnings-as-errors, and submodules.
- `Hexalith.FrontComposer/_bmad-output/project-context.md` - FrontComposer-specific implementation, accessibility, testing, and submodule rules.
- `Hexalith.FrontComposer/tests/README.md` - bUnit, Playwright, axe, and E2E testing conventions.
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Testing/README.md` - component test host utilities.
- `Hexalith.FrontComposer/Directory.Packages.props` - local Fluent UI Blazor package version.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Created from sprint status backlog item `17-2-recovery-and-feedback-state-grammar`.
- Loaded preflight JSON, sprint status, story lessons, project context, Epic 17 requirements, UX recovery/state grammar requirements, architecture constraints, Story 2.7 artifact, Story 17.1 artifact, FrontComposer project context, FrontComposer package/test/component context, recent git history, and Fluent UI Blazor MCP version compatibility warning.
- No product code implementation performed in this create-story workflow.

### Completion Notes List

- Ready-for-dev story created on 2026-05-20.
- Scope is limited to future web recovery and feedback state grammar over the shared Evidence Packet contract.
- Story explicitly records the Story 2.7 and Story 17.1 dependencies, recovery-state distinctions, accessibility/sanitization requirements, and local Fluent UI Blazor version mismatch with the MCP documentation source.

### File List

- `_bmad-output/implementation-artifacts/17-2-recovery-and-feedback-state-grammar.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/process-notes/predev-hardening-runs.log`

## Change Log

- 2026-05-20: Created ready-for-dev story artifact for Recovery and Feedback State Grammar.

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.
