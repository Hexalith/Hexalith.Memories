---
review_lens: accessibility-and-equivalent-semantics
reviewed: 2026-09-09
targets:
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/planning-artifacts/ux-design-directions.html
verdict: revise-before-binding-cli-accessibility-or-activating-web
severity_counts:
  critical: 0
  high: 4
  medium: 4
  low: 1
---

# Accessibility and Equivalent-Semantics Review

## Overall Verdict

**Revise before treating this legacy package as an enforceable accessibility contract for the active CLI or a release gate for a future web product.** The document has unusually strong accessibility intent: it commits to WCAG 2.2 AA for web, defines the full keyboard trust loop, rejects color-only state, requires focus return, linear graph narration, restrained live regions, forced-colors/reduced-motion support, and structured MCP recovery. The remaining defects are contractual rather than conceptual: the active CLI has no accessible-output rules, FrontComposer's existing shell accessibility primitives are not explicitly inherited, the web evidence plan does not cover several load-bearing WCAG 2.2 checks, and the post-May ingestion timing/state contract has not been reconciled.

No critical finding is raised. The PRD still classifies `Hexalith.Memories.Web` as a conformance specimen rather than an activated product surface (`_bmad-output/planning-artifacts/prd.md:709-716`), and the legacy UX correctly says browser composition and visual accessibility checks become binding only when web work is approved (`_bmad-output/planning-artifacts/ux-design-specification.md:578-582`). The gaps below must nevertheless close before web activation, and the CLI gap applies to the current developer surface now.

## Scope and Method

This lens reviewed:

- the legacy UX contract and its historical HTML direction showcase;
- the current PRD's surface/phase contract, FR53-FR58, and NFR32-NFR36;
- the mandatory repository UX baseline and canonical Memories project context;
- current FrontComposer shell, FC-A11Y, common-experience, and release-evidence guidance.

The review covers CLI and MCP equivalent semantics as well as future responsive web behavior: WCAG 2.2 AA, keyboard operation, focus/order/skip links, accessible names and status announcements, non-color state, contrast, forced colors, reduced motion, reflow/zoom, target size, tabular/graph/diagram alternatives, error recovery, cognitive load, timing/progress, terminal output, and phase binding. It is a contract review, not a claim that the historical mock or a production web route passed browser or assistive-technology testing.

## Critical Findings

None.

## High Findings

### A11Y-01 — The active CLI has no binding accessible-terminal output contract

**Evidence**

- The UX calls the CLI a first-class, keyboard-driven surface and asks for compact explain output, diagnostics, and scriptable formats (`_bmad-output/planning-artifacts/ux-design-specification.md:99-113`), but its accessibility rules are written almost entirely as browser component behavior (`:482-492`, `:1104-1162`).
- The CLI example and lifecycle guidance define *what* evidence appears, not a stable screen-reader-friendly reading order, wrapping behavior, animation/cursor policy, or text equivalent for status decoration (`_bmad-output/planning-artifacts/ux-design-specification.md:422-436`, `:790-802`, `:963-972`).
- The current source requires human-readable, JSON, and table formats plus actionable recovery and exit-code semantics (`_bmad-output/planning-artifacts/prd.md:1059-1066`). JSON and exit codes are useful machine contracts, but they do not define accessible default terminal presentation.

**Impact**

A CLI implementation can satisfy the UX with ANSI color, Unicode-only badges, updating spinners, cursor-rewritten progress, clipped tables, or evidence fields arranged for sighted scanning. Users of terminal screen readers, magnification, high-contrast themes, redirected output, or narrow terminals could lose scope, state, source, or recovery information on the product's current primary surface.

**Required fix**

Add an **Accessible terminal output** contract to the behavioral spine. Require text labels for every state and retrieval axis; no color, glyph, animation, or cursor position as the sole carrier of meaning; a stable linear reading order of scope → result → sources → reasoning/state → recovery; bounded/wrappable human output; a linear alternative to wide tables; deterministic non-interactive/piped output; and progress/failure lines that remain understandable without an updating spinner. Bind human, table, JSON, stderr, and exit-code representations to the same canonical state and recovery semantics, while allowing surface-appropriate density rather than byte-for-byte parity.

### A11Y-02 — FrontComposer's ready-made shell accessibility primitives are referenced only generically, leaving skip-link and route-focus ownership ambiguous

**Evidence**

- The UX correctly mandates FrontComposer plus Fluent UI Blazor V5 and names keyboard access, focus visibility, labels, and live regions as foundational (`_bmad-output/planning-artifacts/ux-design-specification.md:336-350`). It later specifies a workflow focus order, labelled navigation, predictable overlay focus entry/return, and a full keyboard trust loop (`:952-960`, `:985-993`, `:1018-1026`, `:1108-1116`).
- It never names `FrontComposerShell`, skip links, the `#fc-main-content` / `#fc-nav` targets, or route-level heading focus. FrontComposer already owns the frame, skip links, accessible chrome, keyboard shortcuts, and polite/assertive status primitives (`references/Hexalith.FrontComposer/docs/reference/components/front-composer-shell.md:17-24`, `:102-126`). Its common experience contract additionally requires skip links, route-level heading focus, visible focus, and logical tab order (`references/Hexalith.FrontComposer/_bmad-output/planning-artifacts/ux-experience-2026-07-05.md:107-118`).
- The FC-A11Y contract defines exact skip-target, focus, live-region, naming, keyboard, reduced-motion, and forced-colors invariants and their automated enforcement layers (`references/Hexalith.FrontComposer/_bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md:25-84`).

**Impact**

A downstream implementer can duplicate shell mechanics, omit the route-level focus target, or interpret overlay focus return as the whole focus contract. Keyboard users may have no reliable way to bypass repeated chrome or locate the new route/result after navigation and asynchronous search, even though the selected platform already supplies most of the necessary behavior.

**Required fix**

State that future Memories web surfaces inherit `FrontComposerShell` and the FC-A11Y primitive set without reimplementation. Record the domain-page delta: one focusable route-level heading/main label; logical DOM and tab order matching the trust workflow; skip targets preserved; focus not moved merely because results update; focus moved and announced when an explicit navigation/overlay action warrants it; and all dialogs, drawers, menus, source previews, graph inspectors, and destructive confirmations return focus to a still-present invoking control. Make the shell contract and its conformance diagnostics explicit validation dependencies.

### A11Y-03 — The proposed web test plan cannot yet substantiate the WCAG 2.2 AA / NFR32 gate

**Evidence**

- NFR32 requires WCAG 2.2 AA, the full keyboard trust workflow, visible focus, non-color state, accessible recovery/status announcements, the UX responsive viewports, and an Epic 17 browser/assistive-technology evidence matrix (`_bmad-output/planning-artifacts/prd.md:1182-1189`).
- The UX test plan names automated contrast/names/labels/ARIA/headings/focusability checks, keyboard and state-comprehension passes, and NVDA on Edge or Chrome (`_bmad-output/planning-artifacts/ux-design-specification.md:1120-1142`). It does not require 200% text resize, 400% zoom / 320-CSS-pixel reflow, focus not obscured by sticky chrome or overlays, or evidence for every responsive surface/state. Its minimum test viewport is 360px even though its own responsive range begins at 320px (`:1082-1102`, `:1124-1129`).
- WCAG 2.2 AA requires reflow without lost information/functionality at the 320-CSS-pixel equivalent and requires focused components not to be entirely obscured; a 360px responsive screenshot does not establish either behavior ([W3C SC 1.4.10](https://www.w3.org/WAI/WCAG22/Understanding/reflow.html), [W3C SC 2.4.11](https://www.w3.org/WAI/WCAG22/Understanding/focus-not-obscured-minimum)).
- FrontComposer's release guidance requires dated manual screen-reader and real-device evidence and keeps zoom/reflow, forced-colors, reduced-motion, and cross-AT coverage distinct from automated checks (`references/Hexalith.FrontComposer/docs/accessibility-verification/README.md:41-66`).

**Impact**

The future web gate can be reported as complete from responsive screenshots, axe output, keyboard checks, and one NVDA pairing while content still fails under text enlargement/reflow, focus is hidden by sticky/overlay content, or a required surface/state lacks manual AT evidence. That would contradict NFR32's evidence-matrix wording and turn a broad WCAG promise into an unverifiable aspiration.

**Required fix**

Define a fail-closed evidence matrix keyed by product route or approved specimen, component/surface, representative state, viewport, zoom/text-size condition, theme, input mode, browser, and assistive technology. At minimum, add 200% text resize; 400% zoom / 320-CSS-pixel reflow; focus-not-obscured checks for sticky scope/chrome, drawers, dialogs, and recovery panels; keyboard start/end focus owners; expected accessible name/announcement; color-independent comprehension; reduced-motion and forced-colors results; sanitized artifact path; tester/date; defect/waiver owner; and release disposition. Automated tooling gaps must remain explicit gaps rather than passes, and manual AT evidence must not be inferred from axe or component tests.

### A11Y-04 — Progress, timeout, and delay semantics have drifted from the current ingestion contract

**Evidence**

- The UX requires progress visibility and meaningful live announcements, but only at a generic level (`_bmad-output/planning-artifacts/ux-design-specification.md:790-802`, `:963-972`, `:996-1005`, `:1112-1116`, `:1162`). The onboarding flow uses `queued, extracting, embedding, indexed`, while the generic state section uses `queued, extracting, embedding, indexing` (`:620-640`, `:996-1005`).
- The current PRD defines one canonical product vocabulary—`pending`, `extracting`, `embedding`, `projecting`, `indexed`, `failed`—and distinguishes retry/dead-letter/repair as details (`_bmad-output/planning-artifacts/prd.md:85-97`).
- NFR36 now sets observable freshness expectations: up to 60 seconds for a normal ≤10 KB unit, up to five minutes for a normal ≤1 MB unit, and an explicit `embedding` delay reported by `memories status` during provider throttling (`_bmad-output/planning-artifacts/prd.md:1191-1195`). NFR16 also requires long rebuild progress to remain observable through `status` (`:1141-1148`).
- FrontComposer's status primitive distinguishes polite non-urgent status from assertive errors/action prompts, uses `aria-atomic="true"`, and skips stale first-render announcements (`references/Hexalith.FrontComposer/_bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md:54-60`).

**Impact**

Users and agents cannot reliably distinguish normal waiting, provider throttling, a stale status view, a failed stage, and an operation that has exceeded its promised freshness window. Repeated progress updates may either be silent or flood a screen reader, while CLI users may receive an animation with no durable progress record. The vocabulary mismatch also breaks equivalent semantics across CLI, MCP, and future web.

**Required fix**

Import the canonical state vocabulary and NFR36 timing/condition semantics by reference. For every long operation, define initial acknowledgement, current stage, last-updated time, normal budget or explicitly unknown duration, delay/degradation reason, affected capability, cancellation/dismissal behavior where safe, completion/failure outcome, and next recovery action. When a client or server timeout occurs, preserve scope and recoverable input, say whether work may still be running, and distinguish a safe retry from an action that could duplicate or conflict. Map the same fields to durable CLI progress lines / `status`, typed MCP fields and structured errors, and web status regions. For web, bind non-urgent transitions to polite status, failures or required action to assertive alert, atomic messages to complete meaning, skip-first-render, and coalescing of noisy intermediate updates.

## Medium Findings

### A11Y-05 — Target sizing is advisory and applies only to primary touch actions

**Evidence**

- Tablet guidance says touch targets need "enough spacing," and the implementation guidance says primary touch interactions should use at least 44px only "where practical" (`_bmad-output/planning-artifacts/ux-design-specification.md:1070-1078`, `:1154-1157`). Secondary commands, icon actions, filter-removal controls, row actions, graph nodes/edges, and overflow items have no enforceable minimum or spacing exception.
- WCAG 2.2 AA SC 2.5.8 applies a 24×24 CSS-pixel minimum or defined spacing/equivalent exceptions to pointer targets generally, not only to primary touch actions ([W3C SC 2.5.8](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum)).

**Impact**

Compact cockpit, grid, citation, graph, and mobile-overflow controls can remain technically present but difficult to activate for users with tremor, reduced dexterity, touch imprecision, or magnification.

**Required fix**

Make SC 2.5.8 the binding floor for every author-sized pointer target and document allowed exceptions. Retain 44×44 CSS pixels as the preferred target for primary and high-risk touch actions, but remove "where practical" from the minimum conformance rule. Add measurement evidence at each responsive tier for icon-only actions, dense table rows, filter chips, graph controls, panel close buttons, and destructive confirmation actions.

### A11Y-06 — Table, graph, and diagram alternatives are directionally good but not interaction-complete

**Evidence**

- The UX requires score text equivalents, linear graph narration, accessible JSON/schema views, understandable activity order, proper grid headers/row actions, and keyboard navigation (`_bmad-output/planning-artifacts/ux-design-specification.md:851-915`, `:1056-1058`, `:1110-1114`). These are strong requirements.
- It does not specify captions or accessible names for grids, header association and sort direction, row-action names that include the target, or how column removal/expandable rows preserve header and state association on narrow screens (`:1093-1100`).
- A linear graph narration is required, but synchronization among selected node/edge, keyboard focus, gap/confidence state, source inspection, and recovery actions is undefined (`:873-882`). The five Mermaid journey diagrams are not paired with equivalent numbered journeys that expose every branch and climax in ordinary text (`:614-770`).

**Impact**

A component can technically expose text while leaving comparison, sorting, selection, traversal, and recovery spatial or pointer-dependent. On narrow screens, trust-critical columns can disappear into row details without an understandable relationship to their headers. The planning diagrams themselves are harder to consume without visual graph interpretation.

**Required fix**

For each grid, define its accessible name/caption, row/column header model, sortable-header name and state, target-qualified row actions, keyboard model, and compact transformation with information parity. For graph exploration, require a synchronized ordered path/list or table that exposes nodes, typed edges, chronology, confidence, gaps, authorization/degradation, selection, and the same inspect/recover commands as the visual graph. Add numbered-text equivalents immediately beside journey diagrams; treat graphics as optional illustrations.

### A11Y-07 — Contrast and forced-colors intent lacks a binding evidence contract for custom Memories components

**Evidence**

- The UX inherits Fluent, rejects color-only meaning, requests reduced-motion/high-contrast support, and asks automated checks to verify contrast (`_bmad-output/planning-artifacts/ux-design-specification.md:440-456`, `:482-492`, `:1104-1133`).
- It does not identify the load-bearing foreground/background, focus, border, selected, disabled, graph, or status combinations that need evidence across supported themes, nor does it bind custom components to FrontComposer's HFC1055 forced-colors gate and release-level visual/manual contrast sign-off (`references/Hexalith.FrontComposer/_bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md:78-84`, `:101-108`, `:127-133`).
- The historical showcase is light-only and uses its own colors (`_bmad-output/planning-artifacts/ux-design-directions.html:8-29`). Static calculation gives only 1.70:1 for its neutral control border (`#c8c6c4` against white) and 2.11:1 for its blue selected outline (`#8ab4f8` against white), so it cannot serve as contrast evidence even though its body/status text combinations are generally stronger (`:212-256`, `:295-307`).

**Impact**

An implementer can inherit compliant Fluent defaults yet introduce a custom Evidence Packet, Trust Strip, graph, or selected state whose essential boundary/focus/status distinction disappears in a theme or forced-colors mode. A downstream reviewer has no declared matrix against which to accept those combinations.

**Required fix**

In the visual spine, map every retained visual state to current Fluent 2 semantic roles and name the combinations requiring text, non-text, focus, and selected-state contrast evidence. In the behavioral spine, require state labels/icons independent of color and bind every custom-color exception to HFC1055 plus forced-colors evidence. Validate light, dark if supported, Windows forced colors, custom themes, focus, disabled, selected, warning/error, graph-edge/gap, and adjacent-status combinations; keep the showcase explicitly non-evidentiary.

### A11Y-08 — Accessibility phase language is mostly correct but not mechanically safe

**Evidence**

- The UX says CLI, MCP, and web are all first-class surfaces, then immediately clarifies that this is full-horizon guidance: CLI is MVP, MCP/EventStore is Phase 1.5, and web remains future work (`_bmad-output/planning-artifacts/ux-design-specification.md:99-113`). It separately gates browser composition and visual checks (`:578-582`).
- Its component roadmap nevertheless uses unqualified "Phase 1," "Phase 2," and "Phase 3" headings (`:927-948`).
- The current PRD says surfaces are capability-aligned rather than 100% feature-parity, MCP is Phase 1.5, web is not an MVP product surface, and NFR32/NFR35 activate only when web ships (`_bmad-output/planning-artifacts/prd.md:43-57`, `:709-716`, `:1096-1107`).

**Impact**

Downstream teams can mistake "Phase 1 - Core Trust Components" for authorization to build or certify a web product during the CLI thesis phase, or can defer accessibility work that actually applies now to terminal output and shared evidence contracts. "Equivalent" can also be misread as full command parity instead of semantic parity for capabilities that a surface actually exposes.

**Required fix**

Tag every accessibility requirement and evidence gate by surface and activation: **shared contract / now**, **CLI / MVP**, **MCP / Phase 1.5**, **web specimen / non-product evidence**, or **web product / activated by approved phase**. Rename component phases to non-activating web composition waves. Define cross-surface equivalence as canonical semantics, ordering, omission/degradation disclosure, and recovery for capabilities present on that surface—not identical interaction or unsupported feature parity.

## Low Findings

### A11Y-09 — The historical showcase is not an accessibility reference and should say so before its controls

**Evidence**

- The direction selector places `aria-selected` on ordinary buttons without tab/listbox semantics, `aria-controls`, managed arrow-key navigation, or a focus/announcement response when the displayed section changes (`_bmad-output/planning-artifacts/ux-design-directions.html:470-478`, `:784-800`).
- The visual graph has positioned nodes but no relationships or linear alternative in the DOM, and the status table lacks a caption or target-qualified row actions (`_bmad-output/planning-artifacts/ux-design-directions.html:624-645`, `:680-707`).
- Its sole media query is width-based; no skip link, `prefers-reduced-motion`, or `forced-colors` rule appears (`_bmad-output/planning-artifacts/ux-design-directions.html:440-460`).
- The artifact accurately identifies itself as a static planning approximation, but only after all eight screens (`_bmad-output/planning-artifacts/ux-design-directions.html:780`).

**Impact**

These are historical mock limitations, **not binding product failures**: the artifact is explicitly non-production, and the prose contract contains stronger accessibility requirements. The risk is copy-forward or use of this file as browser/AT evidence.

**Required fix**

Place a prominent top-of-file notice that the artifact is a historical visual-composition study only: do not copy its markup, CSS, JavaScript, ARIA, focus behavior, colors, or component names, and do not cite it as accessibility evidence. State that the current UX spine, `FrontComposerShell`, Fluent UI Blazor V5, FC-A11Y diagnostics, and validated product/specimen behavior win.

## Strengths

- **The web commitment is explicit and correctly framed as correctness.** WCAG 2.2 AA, non-color trust state, keyboard operation, semantic regions, focus return, reduced motion, and forced colors are all named (`_bmad-output/planning-artifacts/ux-design-specification.md:1104-1118`).
- **The complete keyboard trust loop is unusually concrete.** It spans scope selection, query, search, evidence/source/reasoning/graph inspection, recovery, and return (`_bmad-output/planning-artifacts/ux-design-specification.md:1110-1114`); hover-only behavior is prohibited (`:1158-1160`).
- **Status and error recovery are first-class.** Empty, weak, stale, unauthorized, degraded, graph-gap, and compressed states include cause, effect, and next action rather than generic failure (`_bmad-output/planning-artifacts/ux-design-specification.md:996-1005`, `:1039-1050`).
- **Cognitive load is handled through progressive disclosure rather than information removal.** Trust essentials remain first while sources, scoring, graph paths, token-budget detail, and diagnostics expand on demand (`_bmad-output/planning-artifacts/ux-design-specification.md:592-602`, `:1031-1033`, `:1070-1080`).
- **MCP equivalent semantics are strong.** Typed, bounded fields, deterministic omission disclosure, expansion handles, source attribution, token-budget awareness, and structured recovery avoid prose-only agent UX (`_bmad-output/planning-artifacts/ux-design-specification.md:101-111`, `:684-712`, `:895-904`).
- **Several specialized alternatives are already required.** Scores need textual equivalents, graph paths need linear narration, source links need descriptive labels, schema/JSON views need readable alternatives, and timelines need list/table ordering (`_bmad-output/planning-artifacts/ux-design-specification.md:851-915`).
- **Accessible text is treated as a security surface.** Labels, tooltips, announcements, and copied content may not expose secrets, tokens, raw payloads, restricted source details, or tenant-sensitive diagnostics (`_bmad-output/planning-artifacts/ux-design-specification.md:1162-1164`).
- **The phase guard prevents false current-web claims.** The UX and current PRD both keep product web activation separate from CLI/MCP/shared-contract work (`_bmad-output/planning-artifacts/ux-design-specification.md:99-113`, `:578-582`; `_bmad-output/planning-artifacts/prd.md:709-716`).

## Severity Counts

| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 4 |
| Medium | 4 |
| Low | 1 |

## Recommended Resolution Order

1. Define accessible terminal output and canonical async progress semantics for the current CLI.
2. Explicitly inherit `FrontComposerShell` / FC-A11Y and add the route-focus/skip-link delta.
3. Replace the future-web test prose with a fail-closed WCAG 2.2 / responsive / AT evidence matrix.
4. Reconcile NFR36 timing and the canonical ingestion states across CLI, MCP, and web.
5. Complete target-size, table/graph alternative, and contrast/forced-colors contracts.
6. Surface-tag all requirements and mark the historical HTML as non-evidentiary at the top.
