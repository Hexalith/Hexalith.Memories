# Story 17.3: Contract-Aware Web Interaction Patterns

Status: ready-for-dev

## Story

As a developer or operator,
I want forms, filters, navigation, confirmations, command access, overlays, and data grids to preserve tenant scope and evidence context,
so that web interactions remain safe, predictable, and efficient for repeated work.

## Acceptance Criteria

1. Given a form changes tenant, case, ingestion, source filter, graph, token budget, repair, or benchmark configuration, when the user submits it, then validation is contract-aware, tenant and case scope appear near the top, and dangerous or inconsistent changes require explicit acknowledgement.
2. Given search or filtering controls are displayed, when filters are changed, then active filters for axis, source type, freshness, confidence, time range, metadata, graph depth, and evidence state remain inspectable, and the UI indicates when filters narrow scope, broaden scope, exclude axes, or affect confidence.
3. Given the user navigates from an Evidence Packet to a source, graph path, activity item, operator check, or MCP packet, when navigation completes, then tenant/case/search context is preserved and a clear return path remains available.
4. Given an action is destructive, scope-expanding, repair-oriented, or diagnostic-exporting, when confirmation is required, then the dialog or panel names the tenant, case, object, consequence, and recovery or undo expectation before allowing the action.
5. Given advanced users need fast access, when the command palette or command surface is opened, then search, ingest, inspect source, verify tenant, open graph, retry ingestion, export packet, and inspect MCP payload actions are discoverable with accessible labels.
6. Given memory units, sources, ingestion jobs, case activity, tenant checks, backend health, or benchmark results are listed, when data grids render, then they support sorting, filtering, status badges, row actions, and keyboard navigation without hiding trust-critical fields.

## Tasks / Subtasks

- [ ] Task 0 - Confirm contract, scope, and web foundation before implementation (AC: 1-6)
  - [ ] Confirm Story 2.7 has landed the canonical `Contracts.V1` Evidence Packet record set. If it has not, pause implementation or use fixtures only; do not create a web-only contract, filter, state, navigation, or confirmation vocabulary.
  - [ ] Read `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md`, `EvidencePacket`, `EvidencePacketScope`, `EvidencePacketState`, `EvidencePacketOmittedDetails`, `EvidencePacketExpansionHandle`, and `EvidencePacketRecoveryAction` before binding UI behavior.
  - [ ] Read Stories 17.1 and 17.2 before implementation and reuse their Evidence Cockpit, Trust Strip, Scope Header, Recovery Action Panel/Footer, FrontComposer, Fluent UI, accessibility, sanitization, and responsive guardrails.
  - [ ] Read `Hexalith.FrontComposer/_bmad-output/project-context.md`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Infrastructure/Tenancy`, `State/Navigation`, `State/CommandPalette`, `State/DataGridNavigation`, `Components/DataGrid`, `Components/Forms`, and `Components/Layout` before adding new interaction behavior.
  - [ ] Verify the local Fluent UI Blazor package in `Hexalith.FrontComposer/Directory.Packages.props` before copying examples. The local package is `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.2-26098.1`; the available MCP documentation is for `5.0.0.26098`, so local code/tests are authoritative when signatures differ.

- [ ] Task 1 - Add contract-aware form and validation behavior (AC: 1)
  - [ ] Use FrontComposer-native form patterns and Fluent UI inputs/selects/checkboxes/toggles/date controls before custom form controls.
  - [ ] Place tenant and case scope near the top of forms that change search, ingestion, source, graph, token budget, repair, benchmark, or MCP request configuration.
  - [ ] Validate tenant, case, required source fields, permissions, contract enum/range values, and dangerous scope changes before dispatch.
  - [ ] Ensure validation messages are field-associated, actionable, localizable when added to FrontComposer shell resources, and safe for visible text, accessible names, copied text, diagnostics, logs, and snapshots.
  - [ ] Do not route form submission around existing command lifecycle, authorization, tenant context, or diagnostics gates.

- [ ] Task 2 - Implement inspectable search/filter behavior (AC: 2, 6)
  - [ ] Represent active filters for retrieval axis, source type, freshness, confidence, time range, metadata, graph depth, and evidence state as visible chips/badges or equivalent compact controls.
  - [ ] Show whether a filter narrows scope, broadens scope, excludes retrieval axes, changes graph depth, hides stale/conflicting evidence, or affects confidence interpretation.
  - [ ] Reuse existing FrontComposer data-grid filter components such as `FcFilterSummary`, `FcStatusFilterChips`, `FcColumnFilterCell`, `FcFilterResetButton`, and data-grid navigation state before adding Memories-specific wrappers.
  - [ ] Preserve trust-critical fields in compact grids: tenant, case, confidence, freshness, evidence health, source count, recovery state, and scope status must stay visible or reachable without horizontal-scroll-only access.
  - [ ] Empty filtered states must distinguish no match from filtered-out evidence, inaccessible scope, missing source, stale memory, degraded backend, or insufficient evidence where the Evidence Packet allows.

- [ ] Task 3 - Preserve context through navigation and overlays (AC: 3, 4)
  - [ ] Use FrontComposer navigation state, bounded context parsing, route helpers, and shell conventions to preserve tenant/case/search context when opening sources, graph paths, activity items, operator checks, or MCP packets.
  - [ ] Provide a clear return path from every detail view, side panel, drawer, dialog, or full-screen mobile overlay back to the originating Evidence Packet or grid row.
  - [ ] Inspection overlays for source, graph, reasoning, MCP payload, export, and repair flows must preserve underlying packet context and use predictable focus entry/return behavior.
  - [ ] Do not put tenant/case/search context in singleton/static UI services or unscoped browser storage. Respect Blazor Auto, prerender, Server circuit, WASM lifetime, reconnect, and state handoff constraints.
  - [ ] If the contract cannot preserve a specific return target, render a safe generic return path and record the missing contract field as a deferred follow-up rather than inventing hidden state.

- [ ] Task 4 - Add safe confirmation and command access (AC: 4, 5)
  - [ ] Reuse or extend existing FrontComposer confirmation patterns such as `FcDestructiveConfirmationDialog` before creating new dialog mechanics.
  - [ ] For destructive, scope-expanding, repair-oriented, permission-sensitive, diagnostic-exporting, or restricted-detail actions, require confirmation that names tenant, case, target object, consequence, and recovery or undo expectation.
  - [ ] Surface command actions for search, ingest, inspect source, verify tenant, open graph, retry ingestion, export packet, and inspect MCP payload only when the current packet, context, and authorization decision make the action safe.
  - [ ] Use command palette and command-surface patterns from `FcCommandPalette`, `PaletteResult`, `CommandPaletteEffects`, command policy lookup, and authorization services instead of free-form action dispatch.
  - [ ] Advanced commands must have accessible labels, keyboard operation, disabled reasons where unavailable, and bounded diagnostics when rejected.

- [ ] Task 5 - Add focused component, state, and accessibility tests (AC: 1-6)
  - [ ] Add bUnit coverage using `Hexalith.FrontComposer.Testing`, `FrontComposerTestBase`, or the existing `BunitContext` + `AddFluentUIComponents()` pattern as appropriate.
  - [ ] Test contract-aware forms for scope-first field order, validation summary behavior, field-associated messages, acknowledgement requirements, and command-dispatch gating.
  - [ ] Test filter summaries/chips for axis, source type, freshness, confidence, time range, metadata, graph depth, and evidence state, including warnings for scope-broadening or axis-excluding changes.
  - [ ] Test navigation/overlay context preservation and focus return from source, graph, MCP payload, export, and repair inspection flows.
  - [ ] Test command palette/action availability, accessible labels, disabled reasons, and tenant/user scope reset on scope changes.
  - [ ] Test data-grid sorting, filtering, status badges, row actions, keyboard navigation, compact column priority, and reachable trust-critical fields.
  - [ ] Add negative tests proving secrets, bearer tokens, raw payloads, tenant-sensitive diagnostics, local absolute paths, restricted source details, and unsanitized exception text do not render in visible text, accessible labels, copied text, diagnostics, logs, or snapshots.

- [ ] Task 6 - Validate responsive and integration behavior (AC: 1-6)
  - [ ] Run focused unit/bUnit tests for changed Memories web or FrontComposer component/state projects.
  - [ ] If a runnable web surface is added, run Playwright or equivalent browser checks at 360px, 768px, 1024px, and 1440px. Capture evidence that forms, filters, navigation, confirmations, command actions, overlays, and grids preserve scope and trust-critical fields.
  - [ ] Run automated accessibility checks where the repo already supports them. For FrontComposer E2E, use the existing `tests/e2e` axe helper pattern and role/label or `data-testid` selectors, not CSS class selectors or sleeps.
  - [ ] Run `git diff --check`.

## Dev Notes

### Current Implementation State

- Epic 17 is explicitly future web UI scope. This story is valid when web UI work is selected, but it must not weaken CLI/MCP Evidence Packet behavior or pull unrelated MVP work into scope.
- Story 17.1 defines the Evidence Cockpit, Trust Strip, Scope Header, Source Citation Stack, Retrieval Axis Breakdown, and Graph Path Summary composition boundary. Story 17.3 should add interaction mechanics around that cockpit, not replace the cockpit model.
- Story 17.2 defines recovery and feedback state grammar. Forms, filters, confirmations, overlays, command surfaces, and data grids must use that grammar when they display weak, empty, stale, degraded, unauthorized, compressed, or conflicting states.
- Story 2.7 owns the shared Evidence Packet grammar in `Contracts.V1`. Story 17.3 consumes packet scope, state, omitted details, expansion handles, recovery actions, source references, axis evidence, graph summaries, and structured errors. The web layer must not define separate evidence states, filter semantics, confidence labels, recovery codes, or expansion-handle meanings.
- FrontComposer already contains relevant foundations: tenant/user context accessors, tenant-scoped manifest gates, bounded context navigation, command palette state/effects, command authorization, data-grid navigation and persistence, filter summaries/chips, column prioritization, destructive confirmation dialog, settings/dialog launchers, diagnostics panel, pending-command summary, and `Hexalith.FrontComposer.Testing` utilities.
- FrontComposer is a root-level submodule in this application repository. Treat submodule edits as intentional and separately reviewable. Do not initialize nested submodules or run recursive submodule updates.

### Interaction Semantics

- Scope-first means tenant and case are visible before the user submits forms, changes filters, opens details, exports diagnostics, repairs state, or broadens scope.
- Contract-aware validation means the UI validates against typed contracts and known state grammar, not hand-written string lists hidden in components.
- Filters are trust modifiers. A filter that hides graph evidence, excludes an axis, narrows sources, broadens case scope, or hides stale/conflicting evidence must say so near the affected filter summary.
- Navigation is part of evidence integrity. A source, graph path, activity item, operator check, or MCP packet opened from an Evidence Packet must keep enough context for the user to return and understand the original tenant/case/search boundary.
- Confirmations are safety gates, not generic prompts. They must name the tenant, case, object, consequence, and recovery/undo expectation before allowing destructive, scope-expanding, repair-oriented, permission-sensitive, diagnostic-exporting, or restricted-detail actions.
- Command palette entries must be discoverable for advanced users, but visibility must still respect tenant context, user context, authorization decisions, command lifecycle, and safe packet data availability.

### Component Boundaries

- Prefer FrontComposer primitives and extension points before Memories-specific components: annotations, templates, slots, view overrides, existing shell components, then custom components only when needed.
- Candidate local reuse points include:
  - `Infrastructure/Tenancy/*` for tenant/user context and fail-closed scope capture.
  - `State/Navigation/*` for route parsing, persistence, and scope-change handling.
  - `State/CommandPalette/*` and command policy/authorization services for palette and command-surface behavior.
  - `State/DataGridNavigation/*` and `Components/DataGrid/*` for filters, summaries, status chips, column priority, row details, and grid navigation.
  - `Components/Forms/FcDestructiveConfirmationDialog.*` for confirmation anatomy.
  - `Components/Diagnostics/FcCustomizationDiagnosticPanel.*` and `Components/EventStore/FcPendingCommandSummary.*` for bounded feedback and pending-command examples.
- If a Memories web project is introduced outside the FrontComposer submodule, keep package versions centralized and avoid inline `Version` attributes in `.csproj` files.
- If implementation changes FrontComposer submodule files, run tests from the submodule root and keep those changes scoped to this story.

### Accessibility, Localization, and Sanitization Guardrails

- Every form input requires a visible label or equivalent accessible label. Validation errors must be associated with fields and summarized when multiple fields fail.
- Active filters, status badges, command entries, row actions, confirmation consequences, and overlay titles must have accessible names that include the target object when visible text is short.
- Keyboard support must cover the full interaction chain: form entry, validation review, filter changes, grid sorting/filtering, row actions, command palette search/activation, overlay inspection, confirmation, recovery, and return.
- Focus must move into drawers, dialogs, source previews, graph details, MCP inspectors, export panels, and confirmations, then return to the invoking control when closed.
- Do not rely on hover-only interactions. Filter explanations, source previews, graph detail, command help, tooltip-critical labels, and confirmation details must work by keyboard and touch.
- Use Fluent UI and FrontComposer resource/localization patterns for shell-visible strings. Do not hard-code user-facing strings in shared FrontComposer shell code when local resources exist.
- Do not expose secrets, bearer tokens, raw payloads, tenant-sensitive diagnostics, local absolute paths, restricted source details, or unsanitized exception text in visible labels, accessible names, copied text, tooltips, announcements, diagnostics, logs, or snapshots.

### Testing Notes

- Component and state tests should use xUnit, Shouldly, bUnit, NSubstitute, and existing FrontComposer helpers.
- For shell/component tests, prefer `Hexalith.FrontComposer.Testing` or existing `FrontComposerTestBase` patterns. Register Fluent UI components and localization when rendering Fluent primitives.
- For data-grid and command-palette behavior, reuse existing test locations under `Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation`, `State/CommandPalette`, `Components/DataGrid`, `Components/Forms`, and `Components/Layout`.
- Playwright specs should use accessible role/label selectors or `data-testid` contracts. Do not use CSS class selectors, arbitrary text selectors for framework behavior, committed sleeps, or previous-test state.
- Browser/a11y validation should preserve the Epic 17 viewport set: 360px, 768px, 1024px, and 1440px.

### Dependencies and Non-Goals

- Dependency: Story 2.7 Evidence Packet Contract Mapping must provide the contract semantics consumed here. If it is not implemented at dev time, this story should pause or use contract fixtures only until the dependency is available.
- Dependency: Story 17.1 should define or establish the Evidence Cockpit and trust components this story surrounds with interaction behavior.
- Dependency: Story 17.2 should define or establish the recovery and feedback state grammar this story uses for weak, empty, stale, degraded, unauthorized, compressed, and conflicting states.
- Non-goal: no new retrieval algorithm, ingestion workflow, tenant authorization model, MCP server behavior, CLI output contract, benchmark logic, or operator health matrix.
- Non-goal: no new Evidence Packet contract semantics, recovery action semantics, filter taxonomy, or confidence grammar outside the shared contract. If a needed field is missing, record the contract gap for Story 2.7 or a follow-up.
- Non-goal: no broad FrontComposer framework redesign, no Fluent UI package upgrade, no new assertion/test framework, and no nested submodule initialization or recursive submodule update.

### Suggested Validation Commands

```powershell
dotnet test Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --filter "FullyQualifiedName~CommandPalette|FullyQualifiedName~DataGridNavigation|FullyQualifiedName~FcFilter|FullyQualifiedName~FcDestructiveConfirmationDialog|FullyQualifiedName~Navigation"
dotnet test Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj
npm --prefix Hexalith.FrontComposer/tests/e2e test
git diff --check
```

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 17 and Story 17.3 acceptance criteria.
- `_bmad-output/planning-artifacts/ux-design-specification.md` - UX-DR15, UX-DR27 through UX-DR32, form/filter/navigation/modal/command/data-grid/accessibility patterns.
- `_bmad-output/planning-artifacts/architecture.md` - `Contracts.V1`, Evidence Packet ownership, tenant isolation, structured errors, and interface boundaries.
- `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md` - prerequisite Evidence Packet contract story and current contract mapping guidance.
- `_bmad-output/implementation-artifacts/17-1-evidence-cockpit-and-trust-components.md` - Evidence Cockpit, Trust Strip, Scope Header, source, axis, and graph composition guardrails.
- `_bmad-output/implementation-artifacts/17-2-recovery-and-feedback-state-grammar.md` - recovery, feedback, conflict, compression, accessibility, and sanitization guardrails.
- `_bmad-output/project-context.md` - Memories project rules for .NET, contracts, tests, warnings-as-errors, and submodules.
- `Hexalith.FrontComposer/_bmad-output/project-context.md` - FrontComposer-specific implementation, accessibility, testing, and submodule rules.
- `Hexalith.FrontComposer/tests/README.md` - bUnit, Playwright, axe, selector, and E2E testing conventions.
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Testing/README.md` - component test host utilities.
- `Hexalith.FrontComposer/Directory.Packages.props` - local Fluent UI Blazor package version.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Created from sprint status backlog item `17-3-contract-aware-web-interaction-patterns`.
- Loaded preflight JSON, sprint status, story lessons, project context, Epic 17 requirements, UX interaction/accessibility patterns, architecture constraints, Story 2.7 artifact, Stories 17.1 and 17.2 artifacts, FrontComposer project context, FrontComposer package/test/component context, recent git history, and Fluent UI Blazor MCP version compatibility warning.
- No product code implementation performed in this create-story workflow.

### Completion Notes List

- Ready-for-dev story created on 2026-05-20.
- Scope is limited to future web interaction patterns over the shared Evidence Packet contract and FrontComposer/Fluent UI foundations.
- Story explicitly records the Story 2.7, 17.1, and 17.2 dependencies, tenant/case scope preservation, validation/filter/navigation/confirmation/command/data-grid guardrails, accessibility/sanitization requirements, and local Fluent UI Blazor version mismatch with the MCP documentation source.

### File List

- `_bmad-output/implementation-artifacts/17-3-contract-aware-web-interaction-patterns.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/process-notes/predev-hardening-runs.log`

## Change Log

- 2026-05-20: Created ready-for-dev story artifact for Contract-Aware Web Interaction Patterns.

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.
