# Epic 17 Context: Future Web UX Composition & Accessibility

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Epic 17 establishes the future Hexalith.Memories web experience as a FrontComposer and Fluent UI Blazor V5 composition surface for inspecting evidence, scope, sources, graph context, case activity, operator health, benchmark results, and MCP packets. It matters because the web surface must preserve the same trust model as CLI JSON and MCP responses while adding human-friendly inspection, recovery, responsive behavior, and accessibility evidence. This epic remains future web UI work unless an approved sprint change pulls it into an implementation phase.

## Stories

- Story 17.1: Evidence Cockpit and Trust Components
- Story 17.2: Recovery and Feedback State Grammar
- Story 17.3: Contract-Aware Web Interaction Patterns
- Story 17.4: Role-Specific Web Inspection Lenses
- Story 17.5: Responsive and Accessible Web Validation
- Story 17.6: FrontComposer and Fluent UI Blazor V5 Conformance Hardening
- Story 17.7: Runnable Web Specimen and Browser/AT Accessibility Gap Closure

## Requirements & Constraints

The web RCL must present the shared Evidence Packet without inventing new confidence, state, omitted-detail, source, graph, or recovery semantics. Tenant and case scope must be visible before query, briefing, result, or action content so users can evaluate whether the evidence is from the intended boundary.

Trust-critical state must be explicit and recoverable. Empty, weak, stale, degraded, unauthorized, compressed, and conflicting evidence states need a clear title, explanation, diagnostic clue, severity, affected capability, and one safest recovery action, with optional secondary actions kept subordinate. No-result states must distinguish likely causes when response data allows, including no match, not ingested yet, wrong case, inaccessible scope, stale memory, degraded backend, graph gap, and insufficient evidence.

Forms, filters, navigation, confirmations, command access, overlays, and data grids must preserve tenant, case, and search context. Actions that are destructive, scope-expanding, repair-oriented, or diagnostic-exporting must require deliberate confirmation that names the tenant, case, object, consequence, and recovery or undo expectation.

Role-specific lenses must expose the same evidence model at different densities: case activity, ingestion lifecycle, operator health, benchmark comparison, and MCP request/response inspection. These lenses must avoid exposing secrets, restricted diagnostics, bearer tokens, raw sensitive payload fragments, local absolute paths, provider internals, stack traces, or restricted source details.

Responsive and accessibility validation must cover the trust workflows at 360px, 768px, 1024px, and 1440px. Trust fundamentals must remain reachable without requiring horizontal scrolling. Automated checks must cover accessible names, ARIA validity, heading order, focusable controls, labels, and contrast where the local tooling supports it. Human checks must cover keyboard-only use, focus order, screen-reader readability, no-color-only comprehension, reduced motion, and high-contrast or forced-colors behavior.

## Technical Decisions

`Hexalith.Memories.Web` is a FrontComposer-aligned Razor component library over `Contracts.V1` Evidence Packet semantics. It is not a production web application commitment by itself, and Epic 17 must not introduce backend, ingestion, search, storage, CLI, MCP, public API, or tenant-isolation policy changes unless another approved story explicitly does so.

`Contracts.V1` owns the Evidence Packet grammar shared by CLI JSON, MCP tool responses, and future web composition. The envelope includes scope, result, sources, evidence, graph, state, omitted details, and recovery actions. Lower-level retrieval contracts remain retrieval contracts; the web surface composes them through the Evidence Packet instead of redefining trust semantics.

All web implementation is FrontComposer-first and Fluent UI Blazor V5-only. Existing FrontComposer shell/composition primitives and Fluent components must be used before creating Memories-specific wrappers. Raw semantic markup or scoped CSS is allowed only for unavoidable container or layout gaps with no component equivalent. Any exception requires a conformance allowlist entry naming the file or selector, reason, missing primitive, owner story, and removal condition.

Styling must use Fluent UI V5 component parameters and Fluent 2 tokens where tokenized styling is needed. Components must not recreate theme primitives, direct typography ramps, foreground roles, one-off status color systems, controls, focus treatment, legacy Fluent v4 tokens, or FAST token usage in scoped CSS. Implementation must follow the centrally pinned Fluent UI Blazor V5 package and the aligned FrontComposer submodule rather than copying incompatible examples.

Story 17.7 uses a test/specimen host to make the RCL runnable for browser-backed validation. That host is for fixture routes and evidence generation only; it must reuse existing contract fixtures and avoid new Evidence Packet semantics, backend dependencies, product workflows, public APIs, package upgrades, or FrontComposer framework changes.

## UX & Interaction Patterns

The chosen web direction is one evidence model with task-specific views. Evidence Cockpit is the default view for search and verification; Case Activity Trail supports continuity; Agent Packet Inspector supports schema and MCP payload inspection; Operator Console supports tenant, backend, ingestion, isolation, and repair workflows.

Every Evidence Packet view must preserve the trust fundamentals: scope, source, reasoning, state, and recovery. The Trust Strip should show tenant, case, confidence state, freshness state, source count, evidence health, and token-budget indicator when applicable. If a compact view hides detail, it must show where to expand it.

Use progressive disclosure throughout. The first view should answer what was found, why it appeared, where it came from, what scope was used, how strong and fresh the evidence is, and what action is safest next. Source snippets, retrieval-axis scoring, graph paths, activity history, token-budget details, backend diagnostics, and raw schema views should expand only when needed.

Use Fluent and FrontComposer interaction patterns for command bars, buttons, menus, tabs, breadcrumbs, data grids, forms, badges, banners, inline messages, drawers, dialogs, tooltips, and navigation. Trust states must never rely on color alone. Focus must move predictably into overlays and return to the invoking control. Hover-only behavior is not acceptable for source preview, graph detail, recovery, tooltip, command, or status interactions.

## Cross-Story Dependencies

Story 17.1 establishes the Evidence Cockpit and core trust components that later stories extend. Story 17.2 defines the recovery and feedback grammar reused by interaction patterns, role-specific lenses, and accessibility validation. Story 17.3 depends on the same tenant-aware context and evidence-state semantics when forms, filters, navigation, confirmations, commands, overlays, and grids change scope or expose diagnostics.

Story 17.6 is the conformance gate for the host-less web RCL. Any future Epic 17 implementation or reopened Story 17.2 through 17.5 work must verify Story 17.6 completion evidence first and reuse its conformance tests. Story 17.7 then depends on Story 17.6 evidence to close the browser, axe, forced-colors, reduced-motion, zoom/reflow, touch-target, screen-reader, live focus-trap, and focus-return gaps through a runnable specimen and archived validation evidence.
