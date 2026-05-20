# Story 17.4: Role-Specific Web Inspection Lenses

Status: ready-for-dev

## Story

As a developer, operator, team lead, or LLM-agent integrator,
I want dedicated inspection lenses for case activity, ingestion lifecycle, operator health, benchmark results, and MCP packets,
so that each audience can inspect the same evidence model at the right density.

## Acceptance Criteria

1. Given a case has ingestion, search, membership, annotation, health, or source-link activity, when the Case Activity Trail renders, then activity is chronological, source-linked where possible, status-labelled, and scoped to the selected tenant and case.
2. Given ingestion jobs are queued, extracting, embedding, indexing, indexed, failed, retried, or re-ingested, when the Ingestion Lifecycle Tracker renders, then each unit shows its stage, outcome, retry state, failure details when present, and recovery action when safe.
3. Given tenant verification, backend health, consistency repair, degradation, or ingestion health is inspected, when the Operator Health Matrix renders, then it shows per-check status, affected capabilities, evidence, and next action without exposing secrets or restricted diagnostics.
4. Given benchmark validation has run, when the Benchmark Result Comparator renders, then it shows hybrid-vs-single-axis NDCG@10 results, the 80% thesis threshold status, per-query breakdowns, and links to reproducible evidence.
5. Given MCP requests or responses are inspected, when the Agent Packet Inspector renders, then it shows request summary, response schema, token budget, omitted fields, expansion handles, structured errors, copy controls, and readable schema/JSON views.

## Tasks / Subtasks

- [ ] Task 0 - Confirm contract, scope, and prior web foundations before implementation (AC: 1-5)
  - [ ] Confirm Story 2.7 has landed the canonical `Contracts.V1` Evidence Packet record set. If it has not, pause implementation or use fixtures only; do not create web-only evidence, state, recovery, or schema semantics.
  - [ ] Read `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md`, Story 17.1, Story 17.2, and Story 17.3 before implementing any lens.
  - [ ] Treat all role-specific lenses as different densities over the same evidence model: scope, source, reasoning, state, recovery, freshness, confidence, omitted details, and degraded behavior.
  - [ ] Map each implemented lens to the upstream contract or component source it consumes before adding UI behavior.
  - [ ] Read `Hexalith.FrontComposer/_bmad-output/project-context.md` and reuse FrontComposer tenant/user context, command lifecycle, data grid, diagnostics, forms, navigation, and layout primitives before creating Memories-specific wrappers.
  - [ ] Verify the local Fluent UI Blazor package in `Hexalith.FrontComposer/Directory.Packages.props` before copying examples. The local package is `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.2-26098.1`; the available MCP documentation is for `5.0.0.26098`, so local code/tests are authoritative when signatures differ.

- [ ] Task 1 - Implement Case Activity Trail lens (AC: 1)
  - [ ] Render ingestion, search, membership, annotation, source-link, health, repair, refresh, stale, and deletion activity in chronological order.
  - [ ] Keep tenant and case scope visible before the activity list and preserve scope through filters, details, and row actions.
  - [ ] Link activity rows to source, memory unit, Evidence Packet, graph path, or case object when the contract exposes a safe identifier.
  - [ ] Label every activity status with visible text and an accessible name. Do not rely on color, icon, timeline position, or badge appearance alone.
  - [ ] Render missing, redacted, unauthorized, stale, or deleted source links as explicit states rather than broken links or silent omissions.
  - [ ] Use FrontComposer data-grid, timeline/list, status badge, command, and navigation patterns before adding a custom trail component.

- [ ] Task 2 - Implement Ingestion Lifecycle Tracker lens (AC: 2)
  - [ ] Show each unit's ingestion stage: queued, extracting, extracted, embedding, indexing syntactic, indexing vector, indexing graph, verifying, indexed, failed, retried, re-ingested, compensated, or partially indexed where the contract exposes it.
  - [ ] Show outcome, retry count/state, failure category, safe failure summary, affected capability, and recovery action when available.
  - [ ] Distinguish pending ingestion, failed extraction, embedding provider failure, syntactic/vector/graph indexing degradation, consistency verification failure, and compensation state without exposing raw payloads or secrets.
  - [ ] Route retry, inspect source, inspect failed stage, verify consistency, and repair actions through existing command/navigation conventions with tenant and case context.
  - [ ] Use live-region behavior only for meaningful stage transitions such as failure, retry scheduled, verification complete, or indexed; avoid noisy updates for every progress tick.

- [ ] Task 3 - Implement Operator Health Matrix lens (AC: 3)
  - [ ] Render tenant verification, isolation status, backend health, consistency repair, ingestion health, degraded axis, queue/backlog, rate-limit, and service availability checks in a compact matrix.
  - [ ] For each check, show status, affected capability, evidence summary, last checked time if available, recovery action, and safe diagnostics.
  - [ ] Treat tenant isolation failure, unauthorized scope, backend unavailable, schema mismatch, and cross-tenant ambiguity as trust-blocking states, not decorative warnings.
  - [ ] Do not expose connection strings, bearer tokens, embedding keys, local absolute paths, raw stack traces, tenant-sensitive diagnostics, provider internals, or serialized packets in visible text, accessible labels, copied text, logs, or snapshots.
  - [ ] Use FrontComposer diagnostics, status, command, and data-grid patterns; do not add direct Redis, FalkorDB, DAPR, or infrastructure coupling to web components.

- [ ] Task 4 - Implement Benchmark Result Comparator lens (AC: 4)
  - [ ] Show hybrid-vs-single-axis NDCG@10 results, the 80% thesis threshold status, per-query breakdowns, corpus or fixture identifier, run metadata, and links to reproducible evidence when available.
  - [ ] Preserve benchmark semantics from the contract or existing benchmark output. Do not invent a web-only benchmark score, axis taxonomy, confidence grammar, or threshold.
  - [ ] Render regression, inconclusive, missing baseline, stale benchmark, degraded axis, and unreproducible evidence states explicitly.
  - [ ] Keep charts and score bars paired with text equivalents, table values, accessible labels, and keyboard-reachable detail rows.
  - [ ] Route export, compare run, inspect query, inspect source, and open evidence actions through existing command/navigation conventions.

- [ ] Task 5 - Implement Agent Packet Inspector lens (AC: 5)
  - [ ] Render MCP request summary, tool/resource name, tenant/case scope, response schema, token budget, omitted fields, expansion handles, structured errors, source references, and recovery guidance from the shared Evidence Packet/MCP contracts.
  - [ ] Provide readable schema and JSON views with keyboard navigation, copy controls, redaction, line wrapping, and deterministic expansion-handle display.
  - [ ] Show compressed, omitted, schema mismatch, tool error, unauthorized, degraded backend, pending expansion, and invalid-response states using the shared state grammar.
  - [ ] Do not require users to inspect raw JSON to understand whether the packet is valid, compressed, failed, or expandable.
  - [ ] Copy controls must sanitize bearer tokens, secrets, raw payloads, tenant-sensitive diagnostics, local paths, and restricted source details.

- [ ] Task 6 - Add focused lens, state, accessibility, and sanitization tests (AC: 1-5)
  - [ ] Add bUnit coverage using `Hexalith.FrontComposer.Testing`, `FrontComposerTestBase`, or existing `BunitContext` + `AddFluentUIComponents()` patterns as appropriate.
  - [ ] Build canonical fixtures from Story 2.7 Evidence Packet examples plus Stories 17.1, 17.2, and 17.3 UI/recovery/interaction examples.
  - [ ] Test Case Activity Trail chronological order, source links, scope labelling, status labels, keyboard row actions, redacted links, and missing-source states.
  - [ ] Test Ingestion Lifecycle Tracker stage rendering, retry state, failure summary, safe recovery actions, live-region updates, and degraded backend distinctions.
  - [ ] Test Operator Health Matrix trust-blocking states, affected capabilities, safe diagnostics, recovery actions, and non-leakage.
  - [ ] Test Benchmark Result Comparator threshold status, per-query breakdowns, axis comparison, text equivalents for charts, reproducible evidence links, and stale/missing/inconclusive states.
  - [ ] Test Agent Packet Inspector schema/JSON readability, copy redaction, omitted fields, expansion handles, token-budget display, structured errors, and invalid packet behavior.
  - [ ] Add tenant-isolation tests proving tenant changes reset or partition lens filters, selected rows, detail panels, copy payloads, command targets, and return paths.
  - [ ] Add negative tests proving secrets, bearer tokens, raw payloads, tenant-sensitive diagnostics, local absolute paths, restricted source details, provider internals, and unsanitized exception text do not render in visible text, accessible labels, copied text, diagnostics, logs, or snapshots.
  - [ ] Verify localized resource usage for user-visible lens titles, status labels, empty states, recovery actions, copy controls, schema errors, benchmark labels, and assistive text added by this story.

- [ ] Task 7 - Validate responsive and integration behavior (AC: 1-5)
  - [ ] Run focused unit/bUnit tests for changed Memories web or FrontComposer component/state projects.
  - [ ] If a runnable web surface is added, run Playwright or equivalent browser checks at 360px, 768px, 1024px, and 1440px for every implemented lens.
  - [ ] At phone and tablet widths, verify each lens keeps tenant, case, trust state, source/evidence, affected capability, and recovery reachable without horizontal-scroll-only access.
  - [ ] Run automated accessibility checks where the repo already supports them. For FrontComposer E2E, use the existing `tests/e2e` axe helper pattern and role/label or `data-testid` selectors, not CSS class selectors or sleeps.
  - [ ] Verify keyboard-only use, focus order, focus return from details/drawers/dialogs, screen-reader names, touch target sizing, forced-colors/high-contrast behavior, reduced-motion parity, and no text overlap.
  - [ ] Run `git diff --check`.

## Dev Notes

### Current Implementation State

- Epic 17 is future web UI scope. This story is valid when web UI work is selected, but it must not pull unrelated MVP work forward or weaken CLI/MCP Evidence Packet behavior.
- Story 17.1 defines the Evidence Cockpit, Trust Strip, Scope Header, Source Citation Stack, Retrieval Axis Breakdown, and Graph Path Summary composition boundary.
- Story 17.2 defines recovery and feedback state grammar for weak, empty, stale, degraded, unauthorized, compressed, and conflicting states.
- Story 17.3 defines contract-aware web interaction patterns for forms, filters, navigation, confirmations, command access, overlays, and data grids.
- Story 17.4 should add role-specific lenses around those foundations, not replace them. Each lens is a camera angle over the same evidence, scope, state, and recovery model.
- `Contracts.V1` owns the shared Evidence Packet grammar used by CLI JSON, MCP tool responses, and future web UI composition. The web layer must not define separate evidence states, recovery codes, benchmark thresholds, activity semantics, MCP schema meanings, confidence labels, or expansion-handle meanings.
- FrontComposer already contains relevant foundations: tenant/user context, command lifecycle, command palette, diagnostics panel, pending command summary, data grids, filter/status chips, destructive confirmations, navigation state, layout components, bUnit helpers, and Playwright/axe E2E conventions.
- FrontComposer is a root-level submodule in this application repository. Treat submodule edits as intentional and separately reviewable. Do not initialize nested submodules or run recursive submodule updates.

### Lens Semantics

- The Case Activity Trail is for continuity: how did this case's memory change over time, and which source or Evidence Packet explains the change?
- The Ingestion Lifecycle Tracker is for pipeline recoverability: where is this unit in the ingestion workflow, what failed or degraded, and what is the next safe action?
- The Operator Health Matrix is for operational risk: which tenant, backend, index, graph, queue, consistency, isolation, or ingestion checks are unhealthy, and what capability is affected?
- The Benchmark Result Comparator is for thesis validation: did hybrid retrieval outperform single-axis baselines at the required NDCG@10 threshold, and can the result be reproduced?
- The Agent Packet Inspector is for MCP trust: did the request, response schema, token budget, omitted details, expansion handles, structured errors, and recovery guidance behave correctly?
- Lenses may emphasize different fields, but they must preserve trust fundamentals: scope, source, reasoning, state, recovery, freshness, confidence, omitted details, and degraded behavior.
- If a lens cannot safely show a detail because the contract does not expose it, render unknown, unavailable, redacted, or insufficient evidence and record the gap as a follow-up. Do not infer from raw payloads, logs, exception text, local paths, secrets, browser state, or diagnostics.

### Component Boundaries

- Prefer FrontComposer primitives and extension points before Memories-specific components: annotations, templates, slots, view overrides, existing shell components, then custom components only when needed.
- Shared FrontComposer changes should be minimal, backwards-compatible, and directly tied to the lens being implemented.
- Data-heavy lenses should use Fluent UI/FrontComposer data-grid, list, panel, dialog, command, badge, message, and layout primitives before custom mechanics.
- Lenses must consume typed contracts or typed adapter/view-model results, not arbitrary JSON or stringly typed diagnostics in components.
- Mapping logic belongs in pure adapters or state models. Razor components should render typed lens state and emit command/navigation intents.
- Do not add direct backend, storage, DAPR, Redis, FalkorDB, benchmark-runner, or MCP transport coupling to web components.
- Confirmation or scope-expanding actions must name tenant, case, target object, consequence, and recovery or undo expectation before proceeding.

### Accessibility, Localization, and Sanitization Guardrails

- Every lens title, status, matrix cell, timeline item, stage, benchmark result, schema state, copy action, and recovery action needs visible text or an accessible name.
- Keyboard support must cover the full inspection chain: filter, sort, select row, open detail, copy safe payload, follow recovery, return, and clear selection.
- Focus must move into drawers, dialogs, source previews, MCP inspectors, benchmark detail panels, and health detail panels, then return to the invoking control when closed.
- Dynamic activity, ingestion, health, repair, benchmark, and MCP updates should use live regions only for meaningful state changes; avoid overwhelming assistive technology.
- Use Fluent UI and FrontComposer resource/localization patterns for shell-visible strings. Do not hard-code shared shell display strings when local resources exist.
- Do not expose secrets, bearer tokens, raw payloads, tenant-sensitive diagnostics, local absolute paths, restricted source details, provider internals, serialized packets, or unsanitized exception text in visible labels, accessible names, copied text, tooltips, announcements, diagnostics, logs, or snapshots.
- Charts, matrices, timelines, and JSON/schema views require text equivalents and keyboard navigation. Do not make visual layout the only way to understand order, score, severity, or state.

### Testing Notes

- Component and state tests should use xUnit, Shouldly, bUnit, NSubstitute, and existing FrontComposer helpers.
- For shell/component tests, prefer `Hexalith.FrontComposer.Testing` or existing `FrontComposerTestBase` patterns. Register Fluent UI components and localization when rendering Fluent primitives.
- Playwright specs should use accessible role/label selectors or `data-testid` contracts. Do not use CSS class selectors, arbitrary text selectors for framework behavior, committed sleeps, or previous-test state.
- Browser/a11y validation should preserve the Epic 17 viewport set: 360px, 768px, 1024px, and 1440px.
- Copy/export tests are security tests. Validate redaction and bounded output before validating formatting polish.

### Dependencies and Non-Goals

- Dependency: Story 2.7 Evidence Packet Contract Mapping must provide the contract semantics consumed here. If it is not implemented at dev time, this story should pause or use contract fixtures only until the dependency is available.
- Dependency: Story 17.1 should define or establish the Evidence Cockpit and trust components these lenses reuse.
- Dependency: Story 17.2 should define or establish recovery and feedback state grammar these lenses reuse.
- Dependency: Story 17.3 should define or establish contract-aware navigation, command, confirmation, filter, overlay, and data-grid patterns these lenses reuse.
- Non-goal: no new retrieval algorithm, ingestion workflow, tenant authorization model, MCP server behavior, CLI output contract, benchmark algorithm, or operator repair workflow.
- Non-goal: no new Evidence Packet contract semantics, recovery action semantics, benchmark threshold, MCP schema grammar, activity taxonomy, or confidence grammar outside the shared contract.
- Non-goal: no broad FrontComposer framework redesign, no Fluent UI package upgrade, no new assertion/test framework, and no nested submodule initialization or recursive submodule update.
- Deferred decisions: final lens navigation IA, role-specific default landing views, mobile timeline-vs-table strategy, health matrix grouping, benchmark visualization style, and JSON/schema side-by-side layout require product or architecture approval unless already defined upstream.

### Suggested Validation Commands

```powershell
dotnet test Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --filter "FullyQualifiedName~DataGrid|FullyQualifiedName~Diagnostics|FullyQualifiedName~CommandPalette|FullyQualifiedName~Navigation|FullyQualifiedName~Forms"
dotnet test Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj
npm --prefix Hexalith.FrontComposer/tests/e2e test
git diff --check
```

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 17 and Story 17.4 acceptance criteria.
- `_bmad-output/planning-artifacts/ux-design-specification.md` - Evidence Packet invariants, Case Activity Trail, Agent Packet Inspector, Operator Console, benchmark, responsive, and accessibility patterns.
- `_bmad-output/planning-artifacts/architecture.md` - `Contracts.V1`, Evidence Packet ownership, retrieval contracts, benchmark quality, MCP boundaries, and operational failure propagation.
- `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md` - prerequisite Evidence Packet contract story and current contract mapping guidance.
- `_bmad-output/implementation-artifacts/17-1-evidence-cockpit-and-trust-components.md` - Evidence Cockpit, Trust Strip, Scope Header, source, axis, and graph composition guardrails.
- `_bmad-output/implementation-artifacts/17-2-recovery-and-feedback-state-grammar.md` - recovery, feedback, conflict, compression, accessibility, and sanitization guardrails.
- `_bmad-output/implementation-artifacts/17-3-contract-aware-web-interaction-patterns.md` - interaction, navigation, confirmation, command, data-grid, and tenant-scope guardrails.
- `_bmad-output/project-context.md` - Memories project rules for .NET, contracts, tests, warnings-as-errors, and submodules.
- `Hexalith.FrontComposer/_bmad-output/project-context.md` - FrontComposer-specific implementation, accessibility, testing, and submodule rules.
- `Hexalith.FrontComposer/tests/README.md` - bUnit, Playwright, axe, selector, and E2E testing conventions.
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Testing/README.md` - component test host utilities.
- `Hexalith.FrontComposer/Directory.Packages.props` - local Fluent UI Blazor package version.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Created from sprint status backlog item `17-4-role-specific-web-inspection-lenses`.
- Loaded preflight JSON, automation memory, sprint status, story lessons, project context, Epic 17 requirements, UX inspection-lens/accessibility patterns, architecture Evidence Packet constraints, Stories 17.1 through 17.3 artifacts, FrontComposer project context, FrontComposer package/component context, and Fluent UI Blazor MCP version compatibility warning.
- Preflight had an active-dev-story soft warning for Story 2.7; dirty Story 2.7 artifact and sprint-status implementation changes were left untouched.
- No product code implementation performed in this create-story workflow.

### Completion Notes List

- Ready-for-dev story created on 2026-05-20.
- Scope is limited to future web role-specific inspection lenses over the shared Evidence Packet contract and FrontComposer/Fluent UI foundations.
- Story explicitly records the Story 2.7, 17.1, 17.2, and 17.3 dependencies; role-specific lens boundaries; tenant-scope, accessibility, localization, sanitization, testing, and responsive requirements; and local Fluent UI Blazor version mismatch with the MCP documentation source.

### File List

- `_bmad-output/implementation-artifacts/17-4-role-specific-web-inspection-lenses.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/process-notes/predev-hardening-runs.log`

## Change Log

- 2026-05-20: Created ready-for-dev story artifact for Role-Specific Web Inspection Lenses.

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.
