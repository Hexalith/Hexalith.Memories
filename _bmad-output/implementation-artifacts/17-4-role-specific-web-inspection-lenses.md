---
baseline_commit: a66019d0823f5da48e49be95d558fb3c829f3abd
---

# Story 17.4: Role-Specific Web Inspection Lenses

Status: done

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

## Party-Mode Hardening Clarifications

- Story 17.4 remains a consume-only web slice. Until Story 2.7 is `done` and exposes canonical `Contracts.V1` Evidence Packet fixtures or approved snapshots, implementation may add only typed adapter seams, reusable test host setup, and fixture-backed component tests that do not define new evidence, state, recovery, benchmark, activity, MCP schema, confidence, or expansion-handle semantics.
- All lens semantics must come from the canonical `Hexalith.Memories.Contracts.V1` Evidence Packet, Story 17.1 trust components, Story 17.2 recovery/state grammar, Story 17.3 interaction patterns, or existing FrontComposer primitives. Do not create web-local DTO forks, role-specific packet projections, browser-state-derived evidence, direct backend calls, or infrastructure-specific health semantics.
- Each lens must include a field trace before UI behavior is added: displayed field or state, upstream contract/component source, absent/redacted/degraded/unauthorized behavior, test level, and evidence artifact. When an upstream field is unavailable, render `unknown`, `unavailable`, `redacted`, or `insufficient evidence`; do not infer from raw payloads, logs, local paths, exception text, diagnostics, or provider internals.
- Use a shared lens shell rule across all five lenses: tenant, case, active lens, trust/state/freshness/confidence where available, and return path to the originating Evidence Packet or surface remain visible or reachable through keyboard and compact layouts. Overflow belongs in row details, drawers, tabs, or disclosure patterns rather than horizontal-scroll-only access.
- Default ordering must be deterministic and role-appropriate unless upstream patterns already override it: activity chronological, ingestion by stage/time, health by severity and affected capability, benchmarks by threshold delta and run time, and MCP packets by request time and error state.
- The canonical fixture inventory for this story is bounded to Story 2.7/17.1/17.2/17.3 examples: happy packet, degraded packet, unauthorized packet, redacted packet, omitted/compressed packet, stale packet, invalid/schema-mismatch packet, cross-tenant packet, and missing-source packet. No live Redis, FalkorDB, DAPR, MCP transport, benchmark runner, or backend service is required for lens unit/component coverage.
- Copy, export, diagnostics, logs, accessibility labels, and snapshots are security surfaces. Tests must assert that bearer tokens, secrets, raw payloads, tenant-sensitive diagnostics, local absolute paths, restricted source details, provider internals, serialized packets, and unsanitized exception text are absent from visible text, accessible names, copied text, diagnostics, logs, and snapshots.

## Advanced Elicitation Hardening Clarifications

- Each role-specific lens must be an evidence-density profile over the same canonical packet, not a separate permission model, packet projection, or role-local truth source. Developer, operator, team lead, and LLM-agent integrator labels may change ordering, grouping, and default expansion only when the underlying fields, state grammar, and recovery affordances remain unchanged and traceable.
- Role, lens, tenant, case, packet identity, contract version, and active filter state must be captured together and revalidated before command activation, copy/export, row expansion, drawer open, JSON/schema inspection, benchmark comparison, or recovery navigation. Stale, cross-tenant, missing-contract, unauthorized, or role-mismatch contexts must disable or degrade affected actions with localized reasons instead of executing against an old target.
- Cross-lens consistency is mandatory for trust-critical semantics. The same packet state, source redaction, omitted detail, recovery action, benchmark threshold, MCP schema error, or degraded backend must use the same labels, accessible names, severity, redaction, and unavailable fallback across all five lenses unless an upstream contract explicitly differentiates them.
- Lens-specific secondary surfaces must share one sanitization path with visible UI. Timeline details, ingestion failure summaries, operator diagnostics, benchmark evidence links, MCP schema/JSON views, tooltips, accessible labels, copied text, exported text, logs, screenshots, and snapshots must never be reconstructed from raw payloads, DOM text, backend diagnostics, browser storage, local paths, or provider internals.
- Unknown, future, or partially implemented role/lens states must fail closed. Unknown activity types, ingestion stages, health checks, benchmark metadata, MCP packet fields, expansion handles, row actions, and role-density settings render as unavailable or disabled contract-boundary states with safe explanation and follow-up evidence, not as empty success or silently hidden data.
- If implementation discovers that Story 2.7, 17.1, 17.2, or 17.3 cannot express a needed role-density rule, lens return target, display order basis, benchmark evidence reference, MCP expansion behavior, or recovery consequence, record the gap as a deferred decision or follow-up. Do not add new Evidence Packet, benchmark, MCP, operator-health, authorization, or FrontComposer framework semantics from this story.

## Tasks / Subtasks

- [x] Task 0 - Confirm contract, scope, and prior web foundations before implementation (AC: 1-5)
  - [x] Confirm Story 2.7 has landed the canonical `Contracts.V1` Evidence Packet record set. If it has not, pause implementation or use fixtures only; do not create web-only evidence, state, recovery, or schema semantics.
  - [x] Confirm Story 2.7 status before coding. If Story 2.7 is not `done`, limit work to typed adapter interfaces, approved contract snapshots, reusable lens test host setup, and fixture-backed tests that preserve upstream semantics.
  - [x] Read `_bmad-output/implementation-artifacts/2-7-evidence-packet-contract-mapping.md`, Story 17.1, Story 17.2, and Story 17.3 before implementing any lens.
  - [x] Treat all role-specific lenses as different densities over the same evidence model: scope, source, reasoning, state, recovery, freshness, confidence, omitted details, and degraded behavior.
  - [x] Map each implemented lens to the upstream contract or component source it consumes before adding UI behavior.
  - [x] For each lens, record the field trace table covering displayed fields/states, upstream source, absent/redacted/degraded/unauthorized rendering, test level, and evidence artifact.
  - [x] For every role-density profile, record which fields, sections, default sorting, default expansion, and actions differ by role and prove that the differences do not change authorization, packet semantics, recovery grammar, benchmark threshold, or MCP schema meaning.
  - [x] Define a shared lens-state trace table in code, tests, or developer documentation that names the packet fields, FrontComposer component/state source, authorization source, localized resource keys, redaction path, unavailable fallback, and evidence artifact for each lens.
  - [x] Read `Hexalith.FrontComposer/_bmad-output/project-context.md` and reuse FrontComposer tenant/user context, command lifecycle, data grid, diagnostics, forms, navigation, and layout primitives before creating Memories-specific wrappers.
  - [x] Verify the local Fluent UI Blazor package in `Directory.Packages.props` and `Hexalith.FrontComposer/Directory.Packages.props` before copying examples. The current aligned package is `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`; the available Fluent UI MCP documentation targets `5.0.0.26139` and is incompatible, so local package/submodule code and tests are authoritative when signatures differ.
  - [x] Apply the Epic 17 UX implementation boundary: use FrontComposer and Fluent UI Blazor V5 components/tokens only, and do not add raw HTML/CSS/JavaScript, third-party UI components, legacy Fluent v4/FAST tokens, or handcrafted UI primitives unless the conformance allowlist records an unavoidable gap.

- [x] Task 1 - Implement Case Activity Trail lens (AC: 1)
  - [x] Render ingestion, search, membership, annotation, source-link, health, repair, refresh, stale, and deletion activity in chronological order.
  - [x] Keep tenant and case scope visible before the activity list and preserve scope through filters, details, and row actions.
  - [x] Preserve return navigation to the originating Evidence Packet, case surface, or source-linked activity context.
  - [x] Link activity rows to source, memory unit, Evidence Packet, graph path, or case object when the contract exposes a safe identifier.
  - [x] Label every activity status with visible text and an accessible name. Do not rely on color, icon, timeline position, or badge appearance alone.
  - [x] Render missing, redacted, unauthorized, stale, or deleted source links as explicit states rather than broken links or silent omissions.
  - [x] Treat unknown or future activity types, missing ordering signals, stale packet identity, and cross-tenant activity references as unavailable contract-boundary states with safe labels and disabled unsafe actions.
  - [x] Use FrontComposer data-grid, timeline/list, status badge, command, and navigation patterns before adding a custom trail component.

- [x] Task 2 - Implement Ingestion Lifecycle Tracker lens (AC: 2)
  - [x] Show each unit's ingestion stage: queued, extracting, extracted, embedding, indexing syntactic, indexing vector, indexing graph, verifying, indexed, failed, retried, re-ingested, compensated, or partially indexed where the contract exposes it.
  - [x] Show outcome, retry count/state, failure category, safe failure summary, affected capability, and recovery action when available.
  - [x] Distinguish pending ingestion, failed extraction, embedding provider failure, syntactic/vector/graph indexing degradation, consistency verification failure, and compensation state without exposing raw payloads or secrets.
  - [x] Route retry, inspect source, inspect failed stage, verify consistency, and repair actions through existing command/navigation conventions with tenant and case context.
  - [x] Revalidate tenant, case, unit, stage, role-density profile, and recovery action target before activating retry, inspect, verify, or repair commands; stale or permission-dependent targets must show localized disabled reasons.
  - [x] Keep the current tenant, case, unit, stage, safe recovery action, and return path visible or keyboard-reachable in compact layouts.
  - [x] Use live-region behavior only for meaningful stage transitions such as failure, retry scheduled, verification complete, or indexed; avoid noisy updates for every progress tick.

- [x] Task 3 - Implement Operator Health Matrix lens (AC: 3)
  - [x] Render tenant verification, isolation status, backend health, consistency repair, ingestion health, degraded axis, queue/backlog, rate-limit, and service availability checks in a compact matrix.
  - [x] For each check, show status, affected capability, evidence summary, last checked time if available, recovery action, and safe diagnostics.
  - [x] Treat tenant isolation failure, unauthorized scope, backend unavailable, schema mismatch, and cross-tenant ambiguity as trust-blocking states, not decorative warnings.
  - [x] Do not expose connection strings, bearer tokens, embedding keys, local absolute paths, raw stack traces, tenant-sensitive diagnostics, provider internals, or serialized packets in visible text, accessible labels, copied text, logs, or snapshots.
  - [x] Use the same severity, affected-capability, safe-diagnostics, and recovery-action labels as the shared state grammar when the same degraded or trust-blocking condition appears in other lenses.
  - [x] Use FrontComposer diagnostics, status, command, and data-grid patterns; do not add direct Redis, FalkorDB, DAPR, or infrastructure coupling to web components.
  - [x] Treat operator health as contract/state display only; do not add live infrastructure probes or provider-specific health categories in this story.

- [x] Task 4 - Implement Benchmark Result Comparator lens (AC: 4)
  - [x] Show hybrid-vs-single-axis NDCG@10 results, the 80% thesis threshold status, per-query breakdowns, corpus or fixture identifier, run metadata, and links to reproducible evidence when available.
  - [x] Preserve benchmark semantics from the contract or existing benchmark output. Do not invent a web-only benchmark score, axis taxonomy, confidence grammar, or threshold.
  - [x] Display supplied evidence only. Benchmark scoring semantics, threshold changes, fixture generation, and benchmark-runner execution are outside this story unless already present in canonical fixtures.
  - [x] Render regression, inconclusive, missing baseline, stale benchmark, degraded axis, and unreproducible evidence states explicitly.
  - [x] Preserve contract-provided benchmark ordering, corpus identifiers, run identifiers, threshold basis, and evidence references; when any are missing, render unavailable evidence rather than computing or inferring replacements in the web layer.
  - [x] Keep charts and score bars paired with text equivalents, table values, accessible labels, and keyboard-reachable detail rows.
  - [x] Route export, compare run, inspect query, inspect source, and open evidence actions through existing command/navigation conventions.

- [x] Task 5 - Implement Agent Packet Inspector lens (AC: 5)
  - [x] Render MCP request summary, tool/resource name, tenant/case scope, response schema, token budget, omitted fields, expansion handles, structured errors, source references, and recovery guidance from the shared Evidence Packet/MCP contracts.
  - [x] Provide readable schema and JSON views with keyboard navigation, copy controls, redaction, line wrapping, and deterministic expansion-handle display.
  - [x] Keep readable sections as the primary inspection path; raw JSON, when available, is a secondary view and must preserve the same redaction, keyboard, line-wrap, and copy-safety rules.
  - [x] Show compressed, omitted, schema mismatch, tool error, unauthorized, degraded backend, pending expansion, and invalid-response states using the shared state grammar.
  - [x] Do not require users to inspect raw JSON to understand whether the packet is valid, compressed, failed, or expandable.
  - [x] Treat unknown schema fields, unknown expansion handles, role-inappropriate packet details, stale packet identity, and contract-version mismatches as disabled or unavailable states with safe explanations and no raw payload fallback.
  - [x] Copy controls must sanitize bearer tokens, secrets, raw payloads, tenant-sensitive diagnostics, local paths, and restricted source details.

- [x] Task 6 - Add focused lens, state, accessibility, and sanitization tests (AC: 1-5)
  - [x] Add bUnit coverage using `Hexalith.FrontComposer.Testing`, `FrontComposerTestBase`, or existing `BunitContext` + `AddFluentUIComponents()` patterns as appropriate.
  - [x] Prefer one reusable lens test host/helper over per-lens bespoke setup.
  - [x] Build canonical fixtures from Story 2.7 Evidence Packet examples plus Stories 17.1, 17.2, and 17.3 UI/recovery/interaction examples.
  - [x] Cover the bounded fixture inventory: happy, degraded, unauthorized, redacted, omitted/compressed, stale, invalid/schema-mismatch, cross-tenant, and missing-source packets.
  - [x] For every lens, cover populated, empty, redacted, degraded, unauthorized, and missing/insufficient-evidence states in bUnit or unit tests.
  - [x] Test Case Activity Trail chronological order, source links, scope labelling, status labels, keyboard row actions, redacted links, and missing-source states.
  - [x] Test Ingestion Lifecycle Tracker stage rendering, retry state, failure summary, safe recovery actions, live-region updates, and degraded backend distinctions.
  - [x] Test Operator Health Matrix trust-blocking states, affected capabilities, safe diagnostics, recovery actions, and non-leakage.
  - [x] Test Benchmark Result Comparator threshold status, per-query breakdowns, axis comparison, text equivalents for charts, reproducible evidence links, and stale/missing/inconclusive states.
  - [x] Test Agent Packet Inspector schema/JSON readability, copy redaction, omitted fields, expansion handles, token-budget display, structured errors, and invalid packet behavior.
  - [x] Add tenant-isolation tests proving tenant changes reset or partition lens filters, selected rows, detail panels, copy payloads, command targets, and return paths.
  - [x] Add role/lens switching tests proving role-density changes preserve packet semantics, clear stale selections, revalidate command targets, keep trust-critical labels consistent, and never broaden authorization or expose restricted fields.
  - [x] Add cross-lens consistency tests proving shared packet states, redaction, recovery labels, severity, benchmark threshold status, MCP schema errors, and unavailable fallbacks render equivalently across lenses where the same upstream condition appears.
  - [x] Add stale-context and contract-version tests proving copied text, exports, row expansions, detail drawers, benchmark comparisons, MCP inspection, and recovery commands are disabled or degraded before activation when tenant, case, packet identity, role, or contract version changes.
  - [x] Add unknown/future-value tests for activity types, ingestion stages, health checks, benchmark metadata, MCP fields, expansion handles, role-density settings, and row actions; verify they render safe unavailable states instead of successful empty output.
  - [x] Add negative tests proving secrets, bearer tokens, raw payloads, tenant-sensitive diagnostics, local absolute paths, restricted source details, provider internals, and unsanitized exception text do not render in visible text, accessible labels, copied text, diagnostics, logs, or snapshots.
  - [x] Verify localized resource usage for user-visible lens titles, status labels, empty states, recovery actions, copy controls, schema errors, benchmark labels, and assistive text added by this story.
  - [x] Add an AC-to-test map for each lens, including role navigation, keyboard path, accessible names, focus return, text equivalents, live-region expectations, localization evidence, and non-leakage assertions.

- [x] Task 7 - Validate responsive and integration behavior (AC: 1-5)
  - [x] Run focused unit/bUnit tests for changed Memories web or FrontComposer component/state projects.
  - [x] If a runnable web surface is added, run Playwright or equivalent browser checks at 360px, 768px, 1024px, and 1440px for every implemented lens.
  - [x] Limit Playwright/E2E scope to one smoke path per lens per required viewport when a runnable surface exists; keep state branching in bUnit/unit tests unless the story explicitly adds cross-surface behavior.
  - [x] At phone and tablet widths, verify each lens keeps tenant, case, trust state, source/evidence, affected capability, and recovery reachable without horizontal-scroll-only access.
  - [x] Run automated accessibility checks where the repo already supports them. For FrontComposer E2E, use the existing `tests/e2e` axe helper pattern and role/label or `data-testid` selectors, not CSS class selectors or sleeps.
  - [x] Verify keyboard-only use, focus order, focus return from details/drawers/dialogs, screen-reader names, touch target sizing, forced-colors/high-contrast behavior, reduced-motion parity, and no text overlap.
  - [x] Run `git diff --check`.

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
- 2026-06-24 dev-story implementation: resolved BMAD workflow customization, loaded project context, Hexalith UX instructions, Story 2.7 and Stories 17.1-17.3 artifacts, FrontComposer context, local Fluent package pins, existing Memories web/recovery/interaction components, and current uncommitted Story 17.4 lens work.
- Confirmed Story 2.7 remains `review`, not `done`; implementation stayed consume-only over the canonical `Contracts.V1` Evidence Packet, Story 17.1 trust components, Story 17.2 recovery grammar, Story 17.3 interaction patterns, typed adapters, localization resources, and fixture-backed bUnit/unit tests.
- Validation: `dotnet build tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj --no-restore -m:1` passed with 0 warnings/errors; xUnit v3 in-process web tests passed 256/256 at dev time; `dotnet build Hexalith.Memories.slnx --no-restore -m:1` passed with 0 warnings/errors.
- `dotnet test tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj --no-build --no-restore` was attempted, but VSTest aborted on sandbox local TCP listener permissions (`SocketException (13): Permission denied`); the same assembly was run through the xUnit v3 in-process runner.
- 2026-06-24 review: re-ran the full web test build/suite after the review fixes — `dotnet build ...Web.Tests.csproj -m:1` passed with 0 warnings/errors and the xUnit v3 in-process runner reported 291/291 passing (the dev-time 256 count predated the `LensCrossCuttingTests` cross-cutting suite, which adds 35 cross-lens consistency, tenant-isolation, role-density-invariance, fail-closed, and stale-context tests).

### Completion Notes List

- Ready-for-dev story created on 2026-05-20.
- Scope is limited to future web role-specific inspection lenses over the shared Evidence Packet contract and FrontComposer/Fluent UI foundations.
- Story explicitly records the Story 2.7, 17.1, 17.2, and 17.3 dependencies; role-specific lens boundaries; tenant-scope, accessibility, localization, sanitization, testing, and responsive requirements; and local Fluent UI Blazor version mismatch with the MCP documentation source.
- Implemented the shared lens shell, role-density profiles, and field-trace table so all five lenses keep tenant/case, active lens/role, state, affected capability, confidence/freshness, contract version, and return path visible while preserving packet semantics.
- Implemented Case Activity Trail, Ingestion Lifecycle Tracker, Operator Health Matrix, Benchmark Result Comparator, and Agent Packet Inspector projections/components as consume-only adapters over canonical Evidence Packet fields and Story 17.2/17.3 recovery/interaction outputs.
- Added localization resources and focused unit/bUnit coverage for lens mapping, shell behavior, role-density invariance, traceability, bounded fixture inventory, sanitization/non-leakage, live-region behavior, copy/JSON parity, benchmark unavailable boundaries, and AC-to-test mapping.
- No runnable web host was added by this RCL-only slice; Playwright/axe viewport validation remains not applicable here and is represented by component-level accessibility/markup, keyboard/callback, wrapping/reachability, and localization assertions.

### File List

- `_bmad-output/implementation-artifacts/17-4-role-specific-web-inspection-lenses.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/story-automator/orchestration-2-20260624-144343.md`
- `_bmad-output/process-notes/predev-hardening-runs.log`
- `src/Hexalith.Memories.Web/Components/Lenses/AgentPacket/AgentPacketInspectorMapper.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/AgentPacket/AgentPacketInspectorViewModel.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/AgentPacket/AgentPacketResourceKeys.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/AgentPacket/MemoriesAgentPacketInspector.razor`
- `src/Hexalith.Memories.Web/Components/Lenses/AgentPacket/PacketSchemaField.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/AgentPacket/PacketSchemaFieldKind.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/Benchmark/BenchmarkAxisRow.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/Benchmark/BenchmarkResourceKeys.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/Benchmark/BenchmarkResultComparatorMapper.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/Benchmark/BenchmarkResultComparatorViewModel.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/Benchmark/BenchmarkResultState.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/Benchmark/MemoriesBenchmarkResultComparator.razor`
- `src/Hexalith.Memories.Web/Components/Lenses/CaseActivity/CaseActivityKind.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/CaseActivity/CaseActivityResourceKeys.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/CaseActivity/CaseActivityRow.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/CaseActivity/CaseActivityTrailMapper.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/CaseActivity/CaseActivityTrailViewModel.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/CaseActivity/MemoriesCaseActivityTrail.razor`
- `src/Hexalith.Memories.Web/Components/Lenses/Ingestion/IngestionLifecycleMapper.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/Ingestion/IngestionLifecycleResourceKeys.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/Ingestion/IngestionLifecycleViewModel.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/Ingestion/IngestionOutcome.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/Ingestion/IngestionUnitRow.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/Ingestion/MemoriesIngestionLifecycleTracker.razor`
- `src/Hexalith.Memories.Web/Components/Lenses/LensFieldAvailability.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/LensFieldTrace.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/LensFieldTraceability.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/LensKind.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/LensResourceKeys.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/LensRole.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/LensRoleDensity.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/LensShellMapper.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/LensShellViewModel.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/MemoriesLensShell.razor`
- `src/Hexalith.Memories.Web/Components/Lenses/OperatorHealth/MemoriesOperatorHealthMatrix.razor`
- `src/Hexalith.Memories.Web/Components/Lenses/OperatorHealth/OperatorCheckKind.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/OperatorHealth/OperatorCheckStatus.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/OperatorHealth/OperatorHealthCheckRow.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/OperatorHealth/OperatorHealthMatrixMapper.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/OperatorHealth/OperatorHealthResourceKeys.cs`
- `src/Hexalith.Memories.Web/Components/Lenses/OperatorHealth/OperatorHealthViewModel.cs`
- `src/Hexalith.Memories.Web/Components/Recovery/RecoveryDisplay.cs`
- `src/Hexalith.Memories.Web/Resources/MemoriesWebResources.fr.resx`
- `src/Hexalith.Memories.Web/Resources/MemoriesWebResources.resx`
- `tests/Hexalith.Memories.Web.Tests/Components/Lenses/AgentPacketInspectorMapperTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Lenses/BenchmarkResultComparatorMapperTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Lenses/CaseActivityTrailMapperTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Lenses/IngestionLifecycleMapperTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Lenses/LensCrossCuttingTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Lenses/LensPacketFixtures.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Lenses/LensShellAndTraceabilityTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Lenses/MemoriesLensComponentsTests.cs`
- `tests/Hexalith.Memories.Web.Tests/Components/Lenses/OperatorHealthMatrixMapperTests.cs`

## Change Log

- 2026-05-20: Created ready-for-dev story artifact for Role-Specific Web Inspection Lenses.
- 2026-05-20: Party-mode review applied story hardening for Story 2.7 dependency gating, consume-only Evidence Packet semantics, fixture/test boundaries, role lens shell behavior, navigation continuity, accessibility, localization, and copy/redaction security surfaces.
- 2026-05-20: Advanced elicitation applied story hardening for role-density traceability, stale context revalidation, cross-lens consistency, unknown contract values, and sanitization parity.
- 2026-06-24: Implemented consume-only role-specific inspection lenses, shared lens shell/traceability, localization, focused unit/bUnit coverage, and moved story to review.
- 2026-06-24: Senior Developer Review (AI) — adversarial review passed (clean build, 291/291 tests, verified consume-only contract compliance). Fixed File List omission and stale test count, and extracted the duplicated lens severity→badge-slot mapping into a shared `RecoveryDisplay` helper. Status moved to done.

## Party-Mode Review

- Date: 2026-05-20T12:16:05.8058742+02:00
- Selected story key: `17-4-role-specific-web-inspection-lenses`
- Command/skill invocation used: `/bmad-party-mode 17-4-role-specific-web-inspection-lenses; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), Sally (UX Designer)
- Findings summary:
  - Story 17.4 was directionally valid, but Story 2.7 is still active, so the web lenses needed an explicit consume-only gate that prevents web-local Evidence Packet DTO forks, role-specific packet projections, backend calls, or invented evidence/state/recovery semantics.
  - The story breadth across five lenses could hide implementation and test-platform scope unless bounded to typed adapters, canonical fixtures, reusable test host setup, and representative E2E smoke coverage.
  - Role-specific density, deterministic ordering, navigation return paths, compact-layout behavior, and visible tenant/case trust context needed clearer shared shell expectations before implementation.
  - Copy/export, diagnostics, logs, accessibility labels, and snapshots needed to be treated as security surfaces with explicit non-leakage assertions, not as UI polish.
- Changes applied:
  - Added `## Party-Mode Hardening Clarifications` covering Story 2.7 fixture-only gating, consume-only contract semantics, field trace requirements, shared lens shell behavior, deterministic ordering, bounded fixture inventory, and copy/redaction security surfaces.
  - Tightened Task 0 with Story 2.7 status gating, typed adapter/test-only fallback work, and per-lens field trace mapping before UI behavior.
  - Tightened Tasks 1-5 with return-path, compact-layout, operator-health non-probing, benchmark display-only, and Agent Packet Inspector raw-JSON secondary-view constraints.
  - Tightened Tasks 6-7 with reusable test host guidance, canonical fixture inventory, populated/empty/redacted/degraded/unauthorized state coverage, AC-to-test mapping, localization/accessibility evidence, and bounded E2E smoke scope.
- Findings deferred:
  - Final lens navigation IA, role-specific default landing views, mobile timeline-vs-table strategy, health matrix grouping, benchmark visualization style, JSON/schema side-by-side layout, exact final Story 2.7 field names, benchmark scoring semantics, role labels/permissions, and raw-vs-summarized packet detail remain product or architecture decisions unless already defined upstream.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- Date/time: 2026-05-20T18:10:38+02:00
- Selected story key: `17-4-role-specific-web-inspection-lenses`
- Command/skill invocation used: `/bmad-advanced-elicitation 17-4-role-specific-web-inspection-lenses`
- Batch 1 method names: Red Team vs Blue Team, Security Audit Personas, Failure Mode Analysis, Self-Consistency Validation, Tree of Thoughts
- Reshuffled Batch 2 method names: First Principles Analysis, Pre-mortem Analysis, Architecture Decision Records, Challenge from Critical Perspective, Comparative Analysis Matrix
- Findings summary:
  - Role-specific lenses needed stronger proof that developer, operator, team lead, and LLM-agent integrator density choices do not create separate evidence, authorization, recovery, benchmark, MCP, or packet semantics.
  - Stale role/lens context, copied/exported details, row expansions, drawer targets, benchmark comparisons, and MCP inspection were the highest implementation risks because they can outlive tenant, case, packet, role, or contract-version changes.
  - Trust-critical labels, state precedence, redaction, unavailable fallbacks, benchmark threshold status, MCP schema errors, and recovery labels needed cross-lens consistency guarantees.
  - Unknown or future activity, ingestion, health, benchmark, MCP, expansion-handle, role-density, and row-action values needed fail-closed rendering instead of successful empty states or hidden data.
  - Secondary surfaces needed explicit sanitization parity so visible UI, accessibility labels, copied/exported text, diagnostics, logs, screenshots, and snapshots cannot diverge or leak raw packet data.
- Changes applied:
  - Added `## Advanced Elicitation Hardening Clarifications`.
  - Tightened Task 0 with role-density and shared lens-state traceability tables.
  - Tightened Tasks 1-5 with stale-context revalidation, unknown contract-boundary states, benchmark evidence preservation, MCP contract-version handling, and cross-lens state consistency.
  - Tightened Task 6 with role/lens switching, cross-lens consistency, stale-context, contract-version, unknown/future-value, and sanitization-parity tests.
- Findings deferred:
  - Final role-density defaults, lens navigation IA, mobile timeline/table transformation, health matrix grouping, benchmark visualization, JSON/schema layout, new contract fields, benchmark semantics, MCP expansion behavior, role permission policy, and FrontComposer framework changes remain product or architecture decisions unless already defined upstream.
- Final recommendation: ready-for-dev

## Senior Developer Review (AI)

- Date: 2026-06-24
- Reviewer: Jérôme Piquot (adversarial AI code review, story-automator review flow)
- Outcome: **Approve** (auto-fix mode — all findings resolved in this pass)

### Scope verified

- Read every file in the Dev Agent Record → File List plus the actual git/disk delta (excluding `_bmad/` and `_bmad-output/`).
- Cross-referenced all five Acceptance Criteria and every `[x]` task against the implementation.
- Confirmed build and tests independently: `dotnet build ...Web.Tests.csproj -m:1` → 0 warnings / 0 errors under `TreatWarningsAsErrors=true`; xUnit v3 in-process runner → **291/291 passing**.

### Validated claims (no defects)

- **Consume-only over `Hexalith.Memories.Contracts.V1`.** All five mappers project the canonical `EvidencePacket` only; no web-local DTO forks, role-specific packet projections, browser-derived evidence, backend/DAPR/Redis/FalkorDB/MCP-transport calls, or invented evidence/state/recovery/benchmark/MCP semantics. Fields the contract does not expose (ingestion stage taxonomy, NDCG@10/threshold/per-query, MCP tool name, freshness/last-checked) fail closed to documented unavailable boundaries recorded in `LensFieldTraceability` and deferred to Story 2.7.
- **Reuse of upstream foundations.** Lenses reuse Story 17.1 `EvidenceDisplay`, Story 17.2 `RecoveryStateMapper`/grammar, Story 17.3 `InteractionContextSnapshot`, FrontComposer `FcStatusBadge`, and Fluent UI Blazor V5 primitives — no re-implementation of trust/state/recovery grammar.
- **Security surfaces.** Copy/JSON share one sanitized payload; negative tests assert absence of bearer tokens, secrets (`memory-secret`), raw payloads (`{`/`}`), local paths (`C:\Users\Jerome`), and presence of `[REDACTED]`. Confidence and evidence-existence signals are suppressed under restrictive scope across every lens.
- **Localization.** All 133 referenced resource keys exist in both `MemoriesWebResources.resx` and `MemoriesWebResources.fr.resx` (EN/FR parity); `Localization_EveryLensKeyResolves` enforces this.
- **Fail-closed & cross-lens consistency.** Unknown isolation status is treated as restrictively as unauthorized; unknown/future roles fall back to the safest density; the shared shell renders identical trust context across all five lenses (proven by `LensCrossCuttingTests`).

### Findings and resolutions

| Sev | Finding | Resolution |
| --- | --- | --- |
| MEDIUM | File List omitted `tests/.../Lenses/LensCrossCuttingTests.cs` (a real 17-test file on disk). | Added to the File List. |
| MEDIUM | Debug Log claimed `256/256` web tests; the cross-cutting suite raised the count to `291/291`, so the recorded evidence was stale. | Updated the validation note to `291/291` with an explanation of the delta. |
| LOW | The `SeveritySlot(RecoverySeverity)` switch was duplicated verbatim in all five new lens Razor files, while the repo convention is a shared `EvidenceDisplay`/`InteractionDisplay`-style helper. | Extracted `RecoveryDisplay.SeveritySlot` in the `Recovery` namespace; the five lenses now delegate to it, centralizing the mapping (behavior-preserving — rebuild clean, 291/291 still pass). |

### Notes (no action required)

- AC1 "chronological" ordering is satisfied deterministically by source rank with an explicit "timestamps unavailable" note, because the canonical packet exposes no activity timestamps — an honestly declared contract-boundary deferred to Story 2.7, consistent with the story's hardening clarifications.
- The ingestion/health lenses set `aria-live` on their container with `polite`/`assertive` escalation gated on critical severity; acceptable for a static projection, but worth revisiting if these surfaces later stream live updates.

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created.
Senior Developer Review (AI) completed 2026-06-24 - all findings auto-fixed, story approved and moved to done.
