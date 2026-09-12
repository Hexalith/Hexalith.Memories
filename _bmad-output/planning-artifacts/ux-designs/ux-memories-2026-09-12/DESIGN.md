---
name: Hexalith.Memories
description: Visual identity contract for trustworthy, inspectable memory infrastructure.
status: draft
sources:
  - ../../prd.md
  - ../../ux-design-specification.md
  - ../../ux-design-directions.html
  - ../../ux-validations/ux-memories-2026-09-09/validation-report.md
  - ../../../../references/Hexalith.AI.Tools/hexalith-ux-instructions.md
updated: 2026-09-12
lineage:
  authoritative-prd-sha256: 12579f3a22228348948e805968ea3835732e7ebbcb837e3beef75bd58fc115f1
  rule: This migration derives from the pinned PRD revision; same-day reciprocal PRD links do not reverse authority for this run.
implementation-evidence:
  - .working/extract-ui-implementation.md
  - .working/extract-protocol-implementation.md
  - ../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceAllowlist.cs
  - ../../../../tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ValidationInventory.cs
inheritance:
  application-shell: FrontComposerShell
  application-accessibility: FC-A11Y
  component-library: Microsoft Fluent UI Blazor V5
  visual-language: Fluent 2
  version-owner: central Hexalith.Builds package catalogue
  modes: shell-selected light, dark, forced-colors, density, and system theme
deltas:
  colors: none
  typography: none
  rounded: none
  spacing: none
  elevation: none
colors: {}
typography:
  interface:
    note: Inherit the active Fluent 2 type ramp through FrontComposer.
  technical:
    note: Inherit the Fluent-compatible monospace role for commands, identifiers, paths, JSON, and schema fields.
rounded: {}
spacing: {}
components:
  evidence-cockpit:
    concept: Evidence Cockpit
    implementation: MemoriesEvidenceCockpit
    surface-role: inherited Fluent neutral layered surface
    density: inherited compact application density
    implemented-primitives: FluentAccordion, FluentAccordionItem, FluentMessageBar, FluentText, child Memories components, and registered semantic landmarks
    status-treatment: text plus inherited semantic status
  scope-header:
    concept: Scope Header
    implementation: MemoriesScopeHeader
    surface-role: inherited page-content surface
    density: inherited compact application density
    implemented-primitives: FluentStack, FluentLabel, FcStatusBadge, and registered semantic header/caption markup
    status-treatment: visible tenant, case, and isolation text
  trust-strip:
    concept: Trust Strip
    implementation: MemoriesTrustStrip
    surface-role: inherited page-content surface
    density: inherited compact application density
    implemented-primitives: FluentStack, FluentText, FcStatusBadge, and a registered semantic source-count span
    status-treatment: text plus inherited semantic status
  source-citation-stack:
    concept: Source Citation Stack
    implementation: MemoriesSourceCitationStack
    surface-role: inherited Fluent neutral layer
    density: inherited compact application density
    implemented-primitives: FluentText and registered semantic ordered-list and description-list markup
    status-treatment: packet-backed citation fields and missing-state text; no current action or conflict signal
  retrieval-axis-breakdown:
    concept: Retrieval Axis Breakdown
    implementation: MemoriesRetrievalAxisBreakdown
    surface-role: inherited Fluent neutral layer
    density: inherited compact application density
    implemented-primitives: FluentText and registered semantic ordered-list and description-list markup
    status-treatment: named axis state plus textual score semantics
  graph-path-summary:
    concept: Graph Path Summary
    implementation: MemoriesGraphPathSummary
    surface-role: inherited Fluent neutral layer
    density: inherited compact application density
    implemented-primitives: FluentText and registered semantic description-list and visually-hidden exceptions
    status-treatment: related path identifiers, edge-type list, and gap markers in text
  recovery-action-panel:
    concept: Recovery Action Panel
    implementation: MemoriesRecoveryActionPanel
    surface-role: inherited semantic message surface
    density: inherited compact application density
    implemented-primitives: FluentMessageBar, FluentStack, FluentLabel, FcStatusBadge, FluentButton, and a registered live-region container
    status-treatment: cause, impact, and safest action in text
  evidence-grid:
    concept: Evidence Grid
    implementation: MemoriesEvidenceGrid
    surface-role: inherited full-width data surface
    density: inherited compact data density
    implemented-primitives: FluentDataGrid, PropertyColumn, TemplateColumn, FluentLabel, FcStatusBadge, and FluentButton
    status-treatment: labelled columns and target-qualified row actions
  filter-summary:
    concept: Filter Summary
    implementation: MemoriesFilterSummary
    surface-role: inherited page-content surface
    density: inherited compact application density
    implemented-primitives: FcFilterSummary, FcStatusBadge, FluentLabel, FluentButton, and registered grouping containers
    status-treatment: visible localized filter and sort effect
  interaction-form:
    concept: Interaction Form
    implementation: MemoriesInteractionForm
    surface-role: inherited form surface
    density: inherited comfortable form density
    implemented-primitives: FluentStack, FluentLabel, FluentButton, FluentCheckbox, FcStatusBadge, and semantic form/landmark markup
    status-treatment: inline labelled validation and submit state
  command-surface:
    concept: Command Surface
    implementation: MemoriesCommandSurface
    surface-role: inherited action surface
    density: inherited compact application density
    implemented-primitives: FluentStack, FluentButton, and FluentLabel
    status-treatment: enabled state or readable disabled reason
  action-confirmation:
    concept: Action Confirmation
    implementation: MemoriesActionConfirmation
    surface-role: inherited destructive dialog surface
    density: inherited dialog density
    implemented-primitives: FcDestructiveConfirmationDialog inside a generic wrapper; product activation requires the shell-provided Fluent dialog service and provider lifecycle
    status-treatment: named scope, target, consequence, cancel, and confirm
  context-navigation:
    concept: Context Navigation
    implementation: MemoriesContextNavigation
    surface-role: inherited navigation action surface
    density: inherited compact application density
    implemented-primitives: FluentStack, FluentLabel, and FluentButton
    status-treatment: active scope and destination in text
  shared-lens-shell:
    concept: Shared Lens Shell
    implementation: MemoriesLensShell
    surface-role: inherited detail surface
    density: inherited compact application density
    implemented-primitives: FluentStack, FluentLabel, FcStatusBadge, FluentButton, and registered semantic section/header/footer markup
    status-treatment: packet-derived scope and trust before lens content
  case-activity-trail:
    concept: Case Activity Trail
    implementation: MemoriesCaseActivityTrail
    surface-role: inherited chronological data surface
    density: inherited compact data density
    implemented-primitives: MemoriesLensShell, FluentStack, FluentLabel, and FcStatusBadge
    status-treatment: packet-derived sources, annotation count, graph relations/gaps, trust state, and recovery in text
  ingestion-lifecycle-tracker:
    concept: Ingestion Lifecycle Tracker
    implementation: MemoriesIngestionLifecycleTracker
    surface-role: inherited progress surface
    density: inherited compact application density
    implemented-primitives: MemoriesLensShell, FluentStack, FluentLabel, FluentButton, and FcStatusBadge
    status-treatment: packet-backed source stage and available lifecycle details in text
  operator-health-matrix:
    concept: Operator Health Matrix
    implementation: MemoriesOperatorHealthMatrix
    surface-role: inherited full-width data surface
    density: inherited compact data density
    implemented-primitives: MemoriesLensShell, FluentStack, FluentLabel, FluentButton, and FcStatusBadge
    status-treatment: isolation, authorization, retrieval backend, axis, graph-context, and detail-completeness checks in text
  benchmark-result-comparator:
    concept: Benchmark Result Comparator
    implementation: MemoriesBenchmarkResultComparator
    surface-role: inherited comparison data surface
    density: inherited compact data density
    implemented-primitives: MemoriesLensShell, FluentStack, FluentLabel, and FluentProgressBar
    status-treatment: packet-backed axis NDCG, threshold/pass, run/corpus, per-query hybrid, and evidence-link text alongside every bar
  agent-packet-inspector:
    concept: Agent Packet Inspector
    implementation: MemoriesAgentPacketInspector
    surface-role: inherited diagnostic detail surface
    density: inherited compact technical density
    implemented-primitives: MemoriesLensShell, FluentStack, FluentLabel, FcStatusBadge, FluentButton, and one registered native disclosure
    status-treatment: schema, budget, omissions, errors, and sanitized JSON in text
---

# DESIGN — Hexalith.Memories

## Brand & Style

Hexalith.Memories should feel like dependable infrastructure made inspectable: calm, technical, compact, and operationally honest. The visual hierarchy serves “recoverable trust.” Scope and trust come before the answer; evidence, caveats, omissions, and the safest recovery action remain easy to locate. Emphasis is sparse and reserved for the next safe action, never for decorative AI spectacle.

This is a delta contract over FrontComposer, FC-A11Y, Microsoft Fluent UI Blazor V5, and Fluent 2. The machine-readable <code>inheritance</code> and <code>deltas</code> fields above are normative: Memories introduces no independent theme scale. Each <code>implemented-primitives</code> leaf records the current conformance-tested specimen composition; future product behavior is gated in <code>EXPERIENCE.md</code> and never changes that evidence silently.

## Colors

There are no Memories-owned color tokens. <code>colors: {}</code> is intentional: surfaces, text, borders, focus indicators, interaction states, and semantic statuses use the active Fluent 2 roles selected by FrontComposer. Memories maps meaning to inherited roles but does not restyle <code>FcStatusBadge</code>.

Load-bearing combinations must preserve WCAG 2.2 AA contrast: at least 4.5:1 for normal text, 3:1 for large text and essential UI or graphical boundaries, with equivalent clarity in light, dark, and forced-colors modes. State is always carried by text and semantics as well as color.

## Typography

Use {typography.interface.note} for all application copy and {typography.technical.note} only for commands, identifiers, paths, event or graph IDs, JSON fields, and MCP examples. Preserve the shell’s type ramp, zoom behavior, and user-selected density. Do not introduce a display face, arbitrary font size, or hard-coded line height.

## Layout & Spacing

There are no Memories-owned spacing values. Use Fluent component parameters and current Fluent 2 layout tokens inside FrontComposer page composition.

The visual sequence is stable: Scope Header, Trust Strip, result or primary task, supporting detail, then recovery. Dense read-only grids use the shell’s full-width page mode; prose, forms, and focused details may opt into the shell’s constrained mode. Component-local measure is not a product-page width decision.

On a future web surface, columns collapse before trust content is removed. The Scope Header and Trust Strip wrap and remain before the result. Data grids reduce columns or expose row detail; secondary commands move to accessible overflow; graph content retains an ordered textual equivalent. Responsive behavior is specified in <code>EXPERIENCE.md</code>.

## Elevation & Depth

There is no Memories elevation scale. Use inherited Fluent layer roles and overlays. Depth communicates containment or modal focus, never importance or “AI intelligence.” Do not migrate shadows or layered panel effects from the historical HTML.

## Shapes

There is no Memories radius scale. Buttons, badges, fields, cards, accordions, grids, messages, and dialogs retain current Fluent or FrontComposer shapes. Do not use pill shapes as an unlabelled status code or create decorative geometry around evidence.

## Components

The 19 canonical concepts below are the complete current non-product RCL conformance inventory, not a closed inventory for a future product host. Exact types already exist in the specimen; that evidence does not activate a product route. Visual bindings resolve to the corresponding machine-readable leaf values above.

| Canonical concept | Visual contract |
|---|---|
| Evidence Cockpit | Compose on {components.evidence-cockpit.surface-role} at {components.evidence-cockpit.density}; use {components.evidence-cockpit.implemented-primitives}. Scope Header and Trust Strip stay outside the accordion. |
| Scope Header | Use {components.scope-header.surface-role} with {components.scope-header.implemented-primitives}; keep {components.scope-header.status-treatment}. |
| Trust Strip | Use {components.trust-strip.surface-role} with {components.trust-strip.implemented-primitives}; keep {components.trust-strip.status-treatment}. |
| Source Citation Stack | Use {components.source-citation-stack.surface-role} with {components.source-citation-stack.implemented-primitives}; apply {components.source-citation-stack.status-treatment}. |
| Retrieval Axis Breakdown | Use {components.retrieval-axis-breakdown.surface-role} with {components.retrieval-axis-breakdown.implemented-primitives}; apply {components.retrieval-axis-breakdown.status-treatment}. |
| Graph Path Summary | Use {components.graph-path-summary.surface-role} with {components.graph-path-summary.implemented-primitives}; apply {components.graph-path-summary.status-treatment}. |
| Recovery Action Panel | Use {components.recovery-action-panel.surface-role} with {components.recovery-action-panel.implemented-primitives}; apply {components.recovery-action-panel.status-treatment}. |
| Evidence Grid | Use {components.evidence-grid.surface-role} at {components.evidence-grid.density}; apply {components.evidence-grid.status-treatment}. |
| Filter Summary | Use {components.filter-summary.surface-role} with {components.filter-summary.implemented-primitives}; apply {components.filter-summary.status-treatment}. |
| Interaction Form | Use {components.interaction-form.surface-role} at {components.interaction-form.density}; apply {components.interaction-form.status-treatment}. |
| Command Surface | Use {components.command-surface.surface-role} with {components.command-surface.implemented-primitives}; apply {components.command-surface.status-treatment}. |
| Action Confirmation | Use {components.action-confirmation.surface-role} with {components.action-confirmation.implemented-primitives}; apply {components.action-confirmation.status-treatment}. |
| Context Navigation | Use {components.context-navigation.surface-role} with {components.context-navigation.implemented-primitives}; apply {components.context-navigation.status-treatment}. |
| Shared Lens Shell | Use {components.shared-lens-shell.surface-role} with {components.shared-lens-shell.implemented-primitives}; apply {components.shared-lens-shell.status-treatment}. |
| Case Activity Trail | Use {components.case-activity-trail.surface-role} at {components.case-activity-trail.density}; apply {components.case-activity-trail.status-treatment}. |
| Ingestion Lifecycle Tracker | Use {components.ingestion-lifecycle-tracker.surface-role} with {components.ingestion-lifecycle-tracker.implemented-primitives}; apply {components.ingestion-lifecycle-tracker.status-treatment}. |
| Operator Health Matrix | Use {components.operator-health-matrix.surface-role} at {components.operator-health-matrix.density}; apply {components.operator-health-matrix.status-treatment}. |
| Benchmark Result Comparator | Use {components.benchmark-result-comparator.surface-role} with {components.benchmark-result-comparator.implemented-primitives}; apply {components.benchmark-result-comparator.status-treatment}. |
| Agent Packet Inspector | Use {components.agent-packet-inspector.surface-role} at {components.agent-packet-inspector.density}; apply {components.agent-packet-inspector.status-treatment}. |

The Evidence Cockpit has five sibling titled sections inside one multi-expand accordion: Evidence, Recovery and feedback, Sources, Retrieval axes, and Graph context. Evidence and Recovery begin expanded. Loading and error variants do not render empty subordinate sections. The Agent Packet Inspector’s single native disclosure is a classified, temporary exception for sanitized JSON; it is not a page-section precedent.

## Do's and Don'ts

| Do | Do not |
|---|---|
| Keep tenant or case scope, source, relevance caveat, freshness or degradation, omission, and recovery visually distinct. | Collapse trust into one generic score or “confidence” badge. |
| Use current FrontComposer or Fluent primitives and semantic roles. | Recreate controls, shell chrome, tokens, themes, or overlays with raw markup and CSS when an owned primitive exists. |
| Pair every status, bar, graph relation, and progress signal with text. | Rely on hue, glyph, motion, cursor position, hover, or spatial position alone. |
| Show available, excluded, unavailable, and available-with-no-hits axes as different meanings. | Smooth partial results into a complete-looking answer. |
| Keep graph nodes, edges, paths, gaps, direction, chronology, and case attribution inspectable. | Present an unlabeled decorative graph or imply cross-case traversal. |
| Keep primary actions restrained and recovery adjacent to the affected state. | Add dashboard sprawl, ornamental gradients, novelty palettes, or “AI magic” decoration. |
| Treat the legacy HTML as historical composition evidence only. | Copy its values, markup, script, ARIA, fixed breakpoints, scores, or component names. |
