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

## Advanced Elicitation Hardening Clarifications

- Story 17.3 must bind every interaction family to an upstream contract, FrontComposer primitive, or explicit unavailable fallback before behavior is added. Forms, filters, navigation, overlays, confirmations, commands, and grids may adapt presentation shape, but they must not invent parallel state, scope, filter, recovery, command, or trust semantics.
- Interaction state must be captured as a tenant/case/search-scoped snapshot and revalidated before execution. Scope changes, authorization changes, stale route context, missing packet fields, and contract-version mismatch must disable or degrade affected commands, confirmations, overlays, and grid selections with localized reasons instead of reusing stale targets.
- Filter and grid behavior must preserve evidence meaning under transition and compact states. Unknown or future filter operators, confidence values, evidence states, graph depths, source types, and row action targets must render as unavailable or disabled contract-boundary states rather than being coerced into known labels.
- Confirmation, copy, export, diagnostics, command preview, MCP payload inspection, and accessibility labels must share the same sanitization and redaction path as visible UI. Do not build secondary outputs from raw packet dumps, DOM text, local paths, exception text, browser storage, or diagnostic panels.
- Keyboard, touch, and assistive-technology flows must cover state transitions as well as settled controls: filter changes, tenant/case changes, stale navigation restore, command rejection, confirmation cancel/accept, overlay close, grid row expansion, and focus return all need deterministic evidence.
- If implementation discovers that the current contract cannot express a required interaction consequence, return path, filter effect, command target, or recovery expectation, record the contract gap for Story 2.7 or a follow-up. Do not patch upstream contract, CLI, MCP, retrieval, or FrontComposer framework policy from this story.

## Tasks / Subtasks

- [ ] Task 0 - Confirm contract, scope, and web foundation before implementation (AC: 1-6)
  - [ ] Confirm Story 2.7 has landed the canonical `Contracts.V1` Evidence Packet record set. If it has not, pause implementation or use fixtures only; do not create a web-only contract, filter, state, navigation, or confirmation vocabulary.
  - [ ] Read `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md`, `EvidencePacket`, `EvidencePacketScope`, `EvidencePacketState`, `EvidencePacketOmittedDetails`, `EvidencePacketExpansionHandle`, and `EvidencePacketRecoveryAction` before binding UI behavior.
  - [ ] Read Stories 17.1 and 17.2 before implementation and reuse their Evidence Cockpit, Trust Strip, Scope Header, Recovery Action Panel/Footer, FrontComposer, Fluent UI, accessibility, sanitization, and responsive guardrails.
  - [ ] Read `Hexalith.FrontComposer/_bmad-output/project-context.md`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Infrastructure/Tenancy`, `State/Navigation`, `State/CommandPalette`, `State/DataGridNavigation`, `Components/DataGrid`, `Components/Forms`, and `Components/Layout` before adding new interaction behavior.
  - [ ] Treat this story as a consume-only interaction story. Do not introduce new Evidence Packet grammar, evidence states, filter semantics, confidence labels, recovery codes, expansion-handle meanings, command taxonomy, or trust visual grammar outside definitions owned by Stories 2.7, 17.1, and 17.2.
  - [ ] Map each implemented interaction family (forms, filters, navigation, overlays, confirmations, commands, and grids) to the specific upstream contract or component source it consumes before adding behavior.
  - [ ] Define a traceability table in code, tests, or developer documentation that names the contract fields, FrontComposer state source, authorization source, localized resource keys, and unavailable fallback for every implemented interaction family.
  - [ ] Identify the authoritative existing FrontComposer components, state objects, route helpers, command policies, and test utilities to extend before creating Memories-specific wrappers. Create parallel primitives only when no suitable local API exists and record the reason.
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
  - [ ] Treat unknown or future filter operators, confidence values, evidence states, graph depths, source types, and malformed filter metadata as unavailable contract-boundary states with visible and accessible explanations, not as successful empty results.

- [ ] Task 3 - Preserve context through navigation and overlays (AC: 3, 4)
  - [ ] Use FrontComposer navigation state, bounded context parsing, route helpers, and shell conventions to preserve tenant/case/search context when opening sources, graph paths, activity items, operator checks, or MCP packets.
  - [ ] Provide a clear return path from every detail view, side panel, drawer, dialog, or full-screen mobile overlay back to the originating Evidence Packet or grid row.
  - [ ] Inspection overlays for source, graph, reasoning, MCP payload, export, and repair flows must preserve underlying packet context and use predictable focus entry/return behavior.
  - [ ] Preserve repeated-work context through the existing FrontComposer navigation/state mechanisms where available: active filters, selected packet or source, grid page and sort, expanded evidence, and return location. Do not add new browser-refresh persistence unless an existing tenant-scoped mechanism already supports it.
  - [ ] Do not put tenant/case/search context in singleton/static UI services or unscoped browser storage. Respect Blazor Auto, prerender, Server circuit, WASM lifetime, reconnect, and state handoff constraints.
  - [ ] Revalidate restored route, overlay, command, and grid-row targets against the current tenant, case, authorization decision, packet identity, and contract version before rendering action surfaces.
  - [ ] If the contract cannot preserve a specific return target, render a safe generic return path and record the missing contract field as a deferred follow-up rather than inventing hidden state.

- [ ] Task 4 - Add safe confirmation and command access (AC: 4, 5)
  - [ ] Reuse or extend existing FrontComposer confirmation patterns such as `FcDestructiveConfirmationDialog` before creating new dialog mechanics.
  - [ ] For destructive, scope-expanding, repair-oriented, permission-sensitive, diagnostic-exporting, or restricted-detail actions, require confirmation that names tenant, case, target object, consequence, and recovery or undo expectation.
  - [ ] Surface command actions for search, ingest, inspect source, verify tenant, open graph, retry ingestion, export packet, and inspect MCP payload only when the current packet, context, and authorization decision make the action safe.
  - [ ] Use command palette and command-surface patterns from `FcCommandPalette`, `PaletteResult`, `CommandPaletteEffects`, command policy lookup, and authorization services instead of free-form action dispatch.
  - [ ] Ensure confirmation and command payloads expose bounded contract/evidence identifiers, tenant and case context, and recovery affordance grammar from Story 17.2 without exposing raw payloads or restricted details.
  - [ ] Re-check command and confirmation targets at activation time so stale palette results, changed tenant/case scope, permission changes, or packet reloads cannot execute against an old evidence context.
  - [ ] Advanced commands must have accessible labels, keyboard operation, disabled reasons where unavailable, and bounded diagnostics when rejected.

- [ ] Task 5 - Add focused component, state, and accessibility tests (AC: 1-6)
  - [ ] Add bUnit coverage using `Hexalith.FrontComposer.Testing`, `FrontComposerTestBase`, or the existing `BunitContext` + `AddFluentUIComponents()` pattern as appropriate.
  - [ ] Build a reusable fixture set from canonical Story 2.7 Evidence Packet examples and Story 17.1/17.2 UI/recovery examples covering trusted, degraded, missing, invalid, cross-tenant, and partially loaded contract data. Reuse the same fixture semantics across bUnit and Playwright evidence where practical.
  - [ ] Test contract-aware forms for scope-first field order, validation summary behavior, field-associated messages, acknowledgement requirements, and command-dispatch gating.
  - [ ] Test filter summaries/chips for axis, source type, freshness, confidence, time range, metadata, graph depth, and evidence state, including warnings for scope-broadening or axis-excluding changes.
  - [ ] Test navigation/overlay context preservation and focus return from source, graph, MCP payload, export, and repair inspection flows.
  - [ ] Test command palette/action availability, accessible labels, disabled reasons, and tenant/user scope reset on scope changes.
  - [ ] Test data-grid sorting, filtering, status badges, row actions, keyboard navigation, compact column priority, and reachable trust-critical fields.
  - [ ] Add tenant-isolation interaction tests proving tenant changes reset or partition active filters, saved navigation state, command targets, confirmation payloads, grid selection, and row expansion state.
  - [ ] Add stale-context and version-mismatch tests proving restored routes, cached command results, overlay targets, selected rows, and confirmation payloads are revalidated or disabled before action dispatch.
  - [ ] Add contract-boundary negative tests proving unknown Evidence Packet states, filter operators, confidence values, recovery codes, expansion handles, malformed packet fragments, missing tenant context, stale navigation context, and cross-tenant leakage attempts are rejected, safely ignored, or rendered as upstream-defined degraded/recovery states.
  - [ ] Verify localized resource usage for user-visible validation, confirmation, empty-state, recovery, command, and filter-inspection text added by this story.
  - [ ] Limit snapshot or golden-fixture tests to contract-boundary behavior and sanitized evidence; do not use broad visual snapshots as the primary acceptance proof.
  - [ ] Add negative tests proving secrets, bearer tokens, raw payloads, tenant-sensitive diagnostics, local absolute paths, restricted source details, and unsanitized exception text do not render in visible text, accessible labels, copied text, diagnostics, logs, or snapshots.

- [ ] Task 6 - Validate responsive and integration behavior (AC: 1-6)
  - [ ] Run focused unit/bUnit tests for changed Memories web or FrontComposer component/state projects.
  - [ ] If a runnable web surface is added, run Playwright or equivalent browser checks at 360px, 768px, 1024px, and 1440px. Capture evidence that forms, filters, navigation, confirmations, command actions, overlays, and grids preserve scope and trust-critical fields.
  - [ ] At phone and tablet widths, verify filters, grids, command palette results, overlays, and confirmations remain keyboard/touch reachable without hiding tenant, case, trust, confidence, recovery, or source context.
  - [ ] Run automated accessibility checks where the repo already supports them. For FrontComposer E2E, use the existing `tests/e2e` axe helper pattern and role/label or `data-testid` selectors, not CSS class selectors or sleeps.
  - [ ] Verify one focus contract per interactive surface: initial focus target, tab order, escape/cancel behavior, confirmation behavior, focus return, and screen-reader announcement for filters, commands, confirmations, overlays, and grid row actions.
  - [ ] Include transition-state checks for filter apply/reset, tenant or case switch, stale navigation restore, command rejection, confirmation cancel/accept, overlay close, and grid row expand/collapse.
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
- Contract-aware interaction also means consume-only semantics. Story 17.3 may render or route only states, filters, confidence terms, recovery affordances, expansion handles, and trust indicators defined by Stories 2.7, 17.1, and 17.2.
- Filters are trust modifiers. A filter that hides graph evidence, excludes an axis, narrows sources, broadens case scope, or hides stale/conflicting evidence must say so near the affected filter summary.
- Navigation is part of evidence integrity. A source, graph path, activity item, operator check, or MCP packet opened from an Evidence Packet must keep enough context for the user to return and understand the original tenant/case/search boundary.
- Confirmations are safety gates, not generic prompts. They must name the tenant, case, object, consequence, and recovery/undo expectation before allowing destructive, scope-expanding, repair-oriented, permission-sensitive, diagnostic-exporting, or restricted-detail actions.
- Command palette entries must be discoverable for advanced users, but visibility must still respect tenant context, user context, authorization decisions, command lifecycle, and safe packet data availability.
- Persisted or restored interaction context must be tenant-scoped by existing FrontComposer navigation/storage infrastructure. Browser-refresh persistence, global command palette scope, role-specific command visibility, and mobile grid transformation are deferred decisions unless already implemented locally.

### Component Boundaries

- Prefer FrontComposer primitives and extension points before Memories-specific components: annotations, templates, slots, view overrides, existing shell components, then custom components only when needed.
- Shared FrontComposer changes should be minimal, backwards-compatible, and directly tied to the Story 17.3 interaction surface. Broad framework redesign, Fluent UI upgrades, package version drift, and new assertion/test frameworks are out of scope.
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
- Escape/cancel behavior, confirmation activation, command palette result activation, and screen-reader announcements must be explicit in tests for every changed interaction surface.
- Do not rely on hover-only interactions. Filter explanations, source previews, graph detail, command help, tooltip-critical labels, and confirmation details must work by keyboard and touch.
- Use Fluent UI and FrontComposer resource/localization patterns for shell-visible strings. Do not hard-code user-facing strings in shared FrontComposer shell code when local resources exist.
- Contract-derived terms, recovery labels, confirmation copy, empty states, validation messages, command names, and filter-inspection text must use the same localization path as surrounding FrontComposer UI.
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
- Deferred decisions: browser-refresh persistence for interaction state, mobile grid/card transformation strategy, global versus page-scoped versus role-scoped command palette behavior, invalid upstream contract rendering policy, and any new trust/confidence visual hierarchy require product or architecture approval outside this story unless already defined upstream.

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
- 2026-05-20: Party-mode review hardening applied for consume-only contract boundaries, tenant-isolation evidence, canonical fixtures, accessibility, responsive behavior, localization, and deferred decision guardrails.
- 2026-05-20: Advanced elicitation hardening applied for interaction traceability, stale-context revalidation, transition accessibility, command activation safety, and redaction parity.

## Party-Mode Review

- Date/time: 2026-05-20T11:45:58+02:00
- Selected story key: `17-3-contract-aware-web-interaction-patterns`
- Command/skill invocation used: `/bmad-party-mode 17-3-contract-aware-web-interaction-patterns; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), Sally (UX Designer)
- Findings summary:
  - The story was directionally valid but needed sharper consume-only boundaries so implementation cannot define web-only Evidence Packet states, confidence labels, filter semantics, recovery codes, expansion-handle meanings, command taxonomy, or trust visual grammar.
  - Tenant isolation needed acceptance-level and test-level coverage across forms, filters, navigation restore, command targets, confirmations, grid selection, and row expansion.
  - Test evidence needed canonical fixtures from Stories 2.7, 17.1, and 17.2, plus explicit negative, accessibility, localization, responsive, and focus-contract checks.
  - FrontComposer changes needed a tighter boundary: reuse existing primitives and extension points first, keep shared changes minimal and backwards-compatible, and avoid broad framework redesign or package/test-framework churn.
- Changes applied:
  - Added Task 0 consume-only contract, upstream traceability, and FrontComposer reuse guardrails.
  - Added repeated-work context preservation constraints using existing tenant-scoped FrontComposer state only.
  - Added confirmation and command auditability constraints for bounded evidence identifiers, tenant/case context, and Story 17.2 recovery grammar.
  - Added canonical fixture, tenant-isolation, unknown-contract-value, stale-context, localization, and snapshot-boundary test requirements.
  - Added phone/tablet responsive checks and explicit focus-contract accessibility evidence for filters, commands, confirmations, overlays, and grid row actions.
  - Added Dev Notes clarifying consume-only semantics, tenant-scoped persistence, minimal shared FrontComposer changes, localization, and deferred product/architecture decisions.
- Findings deferred:
  - Browser-refresh persistence for interaction state.
  - Mobile grid versus card-list transformation strategy.
  - Global versus page-scoped versus role-scoped command palette behavior.
  - Invalid upstream contract rendering policy.
  - Any new trust/confidence visual hierarchy or upstream contract grammar.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- Date/time: 2026-05-20T18:06:25+02:00
- Selected story key: `17-3-contract-aware-web-interaction-patterns`
- Command/skill invocation used: `/bmad-advanced-elicitation 17-3-contract-aware-web-interaction-patterns`
- Batch 1 method names: Red Team vs Blue Team, Security Audit Personas, Failure Mode Analysis, Self-Consistency Validation, Tree of Thoughts
- Reshuffled Batch 2 method names: First Principles Analysis, Pre-mortem Analysis, Architecture Decision Records, Challenge from Critical Perspective, Comparative Analysis Matrix
- Findings summary:
  - The story was strong on consume-only boundaries, but interaction implementation still needed sharper traceability from each form, filter, navigation, overlay, confirmation, command, and grid behavior to an upstream contract or FrontComposer source.
  - Stale command targets, restored navigation context, selected grid rows, overlays, and confirmation payloads were the highest implementation risks because they could execute against an old tenant/case/search context.
  - Secondary surfaces such as copy, export, diagnostics, MCP inspection, command preview, accessible labels, and snapshots needed explicit redaction parity with visible UI.
  - Test guidance needed transition-state coverage, not only settled component rendering.
- Changes applied:
  - Added `## Advanced Elicitation Hardening Clarifications`.
  - Added traceability-table guidance for contract, FrontComposer, authorization, localization, and unavailable fallback sources.
  - Added unknown/future filter and metadata handling as unavailable contract-boundary states.
  - Added route, overlay, command, grid-row, and confirmation target revalidation requirements.
  - Added stale-context, version-mismatch, activation-time, transition-state, and redaction-parity test requirements.
- Findings deferred:
  - Browser-refresh persistence for interaction state remains a product/architecture decision unless an existing tenant-scoped mechanism already supports it.
  - Mobile grid/card transformation remains deferred to the responsive validation story or approved UX design.
  - New upstream contract fields for interaction consequences, return paths, filter effects, command targets, or recovery expectations remain Story 2.7 or follow-up work.
  - Global versus page-scoped versus role-scoped command palette policy remains deferred unless already implemented locally.
- Final recommendation: ready-for-dev

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.
