# Validation Report — Hexalith.Memories UX

- **Legacy UX specification:** `_bmad-output/planning-artifacts/ux-design-specification.md`
- **Visual directions:** `_bmad-output/planning-artifacts/ux-design-directions.html`
- **DESIGN.md:** not present
- **EXPERIENCE.md:** not present
- **Run at:** 2026-09-09T09:30:35+02:00

## Overall verdict

**Broken as a downstream UX contract.** The legacy package preserves a strong trust-centered product direction and unusually thoughtful accessibility intent, but it is not a `DESIGN.md` / `EXPERIENCE.md` spine pair, cannot be consumed mechanically as one, covers only part of the current PRD journey set, and has drifted from current phase, command, authorization, scoring, and lifecycle contracts.

The extra lenses reinforce the rubric rather than soften it. The most urgent correction is security semantics: cross-tenant or tenant-claim-mismatched requests must fail closed, never become confirmable scope expansion. Web implementation should remain activation-gated until exact FrontComposer/Fluent ownership and accessibility evidence contracts are written; the active CLI also needs an accessible terminal-output contract now.

## Category verdicts

- Flow coverage — **broken**
- Token completeness — **broken**
- Component coverage — **thin**
- State coverage — **broken**
- Visual reference coverage — **adequate**
- Bloat & overspecification — **thin**
- Inheritance discipline — **broken**
- Shape fit — **broken**

## Severity summary

The consolidated list below deduplicates overlapping findings across lenses.

| Scope | Critical | High | Medium | Low |
|---|---:|---:|---:|---:|
| Consolidated findings | 3 | 17 | 9 | 3 |
| Rubric walker, raw | 4 | 15 | 9 | 2 |
| Source-drift lens, raw | 1 | 10 | 3 | 1 |
| Fluent/FrontComposer lens, raw | 0 | 2 | 2 | 1 |
| Accessibility lens, raw | 0 | 4 | 4 | 1 |

## Findings by severity

### Critical (3)

#### [Shape / Tokens] No peer spine pair or machine-readable visual contract

The single 1,164-line legacy document mixes visual identity, behavior, product narrative, journeys, and implementation guidance. No `DESIGN.md` token tree, `EXPERIENCE.md` behavior spine, or `{path.to.token}` references exist (`ux-design-specification.md:16`, `:438`, `:804`). Downstream consumers cannot select or validate the visual and behavioral contracts independently.

Fix: reconcile current sources, then distill peer `DESIGN.md` and `EXPERIENCE.md` files. Inherit FrontComposer and Fluent UI Blazor V5 explicitly; record only justified visual deltas as typed tokens.

#### [Flow] Current PRD journeys are not traceably covered

The PRD defines J1–J10, while the UX contains five unnumbered Mermaid flows. J6, J8, and J10 are absent; J1/J9 and J3/J7 are blended; J2 and J5 lose key failure paths (`prd.md:297-519`; `ux-design-specification.md:614-770`).

Fix: mirror source journey identifiers and names verbatim. Give each included journey numbered steps, an explicit climax, phase/surface tags, and applicable failure/recovery behavior; explicitly justify infrastructure-only exclusions.

#### [Source Drift / Security] Tenant-boundary failures are modeled as confirmable warnings

The UX permits ambiguous or unauthorized scope to “block or warn,” groups unauthorized and cross-tenant states with confirmable expansion, and offers “switch tenant” recovery (`ux-design-specification.md:133`, `:426`, `:565`, `:656-660`, `:1035-1037`). Current NFR8/NFR11 require principal-driven, fail-closed rejection and no ambient tenant switch (`prd.md:82-93`, `:570-572`, `:875`, `:1123-1130`).

Fix: reserve confirmation for authorized same-tenant scope changes. Tenant-claim mismatch, missing authenticated scope, unauthorized sources, and cross-tenant access must refuse before retrieval without leaking restricted evidence; changing tenant requires a newly authorized identity/session.

### High (17)

#### [Inheritance] Source provenance is stale, broken, and lacks precedence

Legacy frontmatter predates the change-controlled September PRD, omits current decision-log precedence, and references a missing Commons project-context file (`ux-design-specification.md:1-13`; `prd.md:1-5`; `.memlog.md:26-38`).

Fix: use a resolvable `sources:` manifest in both spines, name current revision dates and precedence, label older brief/research inputs historical, and remove or repair the missing path.

#### [Source Drift] Product phases and UX component phases collide

The UX correctly says CLI-first MVP, MCP/EventStore Phase 1.5, and future web, then reuses “Phase 1/2/3” for web component tranches (`ux-design-specification.md:99-113`, `:927-948`). The PRD keeps the Web host as a non-product conformance specimen (`prd.md:709-720`).

Fix: rename the tranches to non-activating web-composition waves and add a product-phase/surface/delivery matrix sourced from the PRD.

#### [Source Drift] CLI examples use obsolete grammar and tenant-switch behavior

The UX and mock use `memories search "…" --case … --explain` and pseudo-command forms, while the current grammar is `memories search query --tenant <t> --query "<q>" [--case <c>] [--axis <a>] [--explain]` (`ux-design-specification.md:422-424`; `ux-design-directions.html:647-655`; `prd.md:871-900`).

Fix: copy the canonical command grammar verbatim into flows, examples, recovery copy, and visual artifacts. Remove tenant-switch language and mark stubbed commands unavailable.

#### [Source Drift] MCP wire names are invented instead of version-bound

The UX alternates `budget` and “token budget,” and the mock invents response fields without a schema/version mapping (`ux-design-specification.md:422-424`, `:684-703`, `:895-904`; `ux-design-directions.html:709-740`).

Fix: reference the active MCP and `Contracts.V1` contracts and map each tool’s phase, request fields, server-controlled fields, response/error type, token-budget behavior, and expansion handle. Do not create wire names in UX prose.

#### [Inheritance] Default explain behavior is unresolved and `UX-DR7` does not exist

The UX makes explain content automatic, but also limits Retrieval Axis Breakdown to explain mode; the PRD retains optional `--explain` and cites an undefined `UX-DR7` (`ux-design-specification.md:83-125`, `:851-860`; `prd.md:1197-1205`).

Fix: resolve the decision through change control and create a stable UX identifier or section anchor. Separate always-present trust fields from opt-in ranks, weights, matched terms, and graph detail.

#### [Inheritance] RRF, graph seeding, and confidence semantics have drifted

The UX describes normalized magnitude-like axis scores and generic confidence. Current sources require weighted reciprocal-rank fusion, top-5 syntactic plus top-5 semantic graph auto-seeding at depth ≤2, and separate relevance, metadata, and edge confidence (`ux-design-specification.md:418`, `:422-430`, `:539-563`, `:851-877`; `prd.md:78-94`, `:158-166`, `:547-597`).

Fix: use the canonical nouns and distinguish single-axis values from hybrid rank contributions. Label relevance as relevance—not factual certainty—and show graph-start provenance and excluded/degraded axes.

#### [State] There is no IA or surface-state closure proof

Named views and components are scattered through the document, but there is no IA table with entry point, purpose, phase, journey, requirements, components, or required states (`ux-design-specification.md:496-525`, `:569-574`, `:816-916`, `:985-1005`).

Fix: add a surface closure matrix and a per-surface state matrix covering applicable cold-load, empty, focus, validation, error, degradation, permission denial, long-running, and recovery behavior.

#### [Components] Visual and behavioral component contracts are co-mingled

The custom component dossiers are useful but do not provide identically named `DESIGN.md.Components` and `EXPERIENCE.md.Component Patterns` rows (`ux-design-specification.md:804-923`).

Fix: split every retained component into visual and behavioral peer entries with canonical names and real rules.

#### [Components] Roadmap components are implementation names without usable contracts

`Ingestion Lifecycle Tracker`, `Operator Health Matrix`, and `Benchmark Result Comparator` lack the anatomy, states, interactions, and ownership supplied for earlier concepts (`ux-design-specification.md:927-948`).

Fix: define complete two-spine rows or mark them as future/inherited concepts and remove them from the implementation-ready inventory.

#### [Fluent] Exact FrontComposer and Fluent V5 ownership is undefined

The prose names generic “command bars,” “drawers,” and “panels,” but the pinned Fluent package does not expose matching types; `FrontComposerShell` is never named even though it owns shell layout, navigation, providers, skip links, theme/settings/account UI, and global commands (`ux-design-specification.md:320`, `:810`, `:954`, `:1020-1025`).

Fix: add a binding ownership table: shell behavior belongs to `FrontComposerShell`; domain content belongs to Memories; actions, feedback, grids, layout, and overlays map to exact pinned V5 or FrontComposer components. Treat any genuine gap as justified custom composition with tests.

#### [Fluent] The mandatory page-section accordion rule is absent

Evidence Packet pages contain multiple sibling titled regions, but the UX never requires the repository-mandated single `FluentAccordion` with one `FluentAccordionItem` per section and the primary item expanded (`ux-design-specification.md:545-554`, `:818-915`, `:1031-1033`; `hexalith-ux-instructions.md:41-51`).

Fix: put titles, breadcrumbs, toolbar, scope/trust context, and a sole primary region outside the accordion; group two or more remaining sibling titled regions in one accordion and expand the primary item by default.

#### [Accessibility] The active CLI lacks an accessible terminal-output contract

The current primary surface has no binding rules for reading order, wrapping, non-color/non-glyph meaning, spinner/cursor behavior, narrow terminals, redirected output, or linear alternatives to wide tables (`ux-design-specification.md:99-113`, `:422-436`, `:790-802`; `prd.md:1059-1066`).

Fix: define stable scope → result → sources → reasoning/state → recovery order; text labels for all state; bounded/wrappable human output; deterministic piped output; durable progress/failure lines; and semantically equivalent human, table, JSON, stderr, and exit-code forms.

#### [Accessibility] FrontComposer shell accessibility inheritance is implicit

The UX never names `FrontComposerShell`, skip targets, route-heading focus, or the FC-A11Y primitive set (`ux-design-specification.md:336-350`, `:952-960`, `:1108-1116`).

Fix: inherit the shell and FC-A11Y contracts explicitly. Record only the domain-page delta: main/heading focus, logical DOM/tab order, non-disruptive result updates, appropriate focus movement, and focus return to a still-present invoker.

#### [Accessibility] The future-web evidence plan cannot prove NFR32

The test plan omits 200% text resize, 400% zoom/320-CSS-pixel reflow, focus-not-obscured checks, and route/state-level evidence coverage; its minimum test viewport is 360px despite a 320px range (`ux-design-specification.md:1082-1142`; `prd.md:1182-1189`).

Fix: create a fail-closed evidence matrix by route/specimen, state, viewport, zoom/text size, theme, input, browser, assistive technology, expected focus/name/announcement, artifact, tester/date, defect/waiver owner, and release disposition.

#### [Accessibility / State] Async progress and timing semantics are stale

The UX uses inconsistent ingestion labels and generic progress announcements. The current contract is `pending → extracting → embedding → projecting → indexed`, terminal `failed`, with retry/dead-letter/repair as details; NFR36 adds timing and throttling disclosure (`ux-design-specification.md:620-640`, `:996-1005`; `prd.md:85-97`, `:1141-1148`, `:1191-1195`).

Fix: import the canonical state/timing vocabulary. Define acknowledgement, current stage, last update, expected/unknown duration, delay reason, affected capability, cancellation, completion/failure, retry safety, and recovery across CLI, MCP, and web announcement primitives.

#### [Visual Contract] The HTML showcase creates an unsafe parallel theme vocabulary

The historical visual uses hard-coded colors, raw controls, custom JavaScript, and legacy-shaped `--neutral-*` variables even though the UX requires Fluent 2 roles and reuse-first implementation (`ux-design-directions.html:7-45`, `:470-478`, `:784-800`; `ux-design-specification.md:338-342`).

Fix: mark the artifact non-normative at the top and beside every link, prohibit copy-forward of markup/CSS/JS/ARIA/tokens, and map retained visual decisions to current Fluent 2 roles in `DESIGN.md`.

#### [Bloat] Source narrative and core rules are duplicated

The artifact repeats product vision, personas, emotional goals, trust principles, state/recovery rules, and “Core User Experience,” preserving a stale May snapshot and weakening extraction (`ux-design-specification.md:25-187`, `:366`, `:539-602`, `:950-1068`).

Fix: retain source identifiers and UX consequences only; establish each invariant once and use tables/references for surface or component deltas.

### Medium (9)

#### [Tokens / Accessibility] Load-bearing contrast and theme decisions are not bound

WCAG AA and forced colors are aspirations without named foreground/background, focus, border, selected, disabled, graph, or status combinations; the showcase is light-only (`ux-design-specification.md:440-492`, `:1104-1133`; `ux-design-directions.html:8-29`).

Fix: map visual states to Fluent 2 semantic roles, name required contrast evidence for supported themes/forced colors, and state the dark-mode decision.

#### [Components] Canonical names and aliases are inconsistent

`Scope strip`/`Trust Strip`, `Recovery footer`/`Recovery Action Panel`, and `evidence state`/`evidence health` drift across the document (`ux-design-specification.md:545`, `:567`, `:818-889`, `:1043-1050`).

Fix: choose one canonical name per component and field and use it across anatomy, states, patterns, flows, sources, and both spines.

#### [Visual References] Artifact placement, inline linkage, and precedence are incomplete

The one HTML artifact is linked only once, remains at planning-root level, does not illustrate a final blended direction, and has no “spines win on conflict” rule (`ux-design-specification.md:496-513`; `ux-design-directions.html:780`).

Fix: classify/promote the artifact, link it at relevant sections with a precise illustration note, state precedence once, and list unmocked surfaces as spine-only.

#### [Source Drift] Evidence Packet overpromises universal synthesis and actions

The surface-neutral packet permits synthesized answers and operator actions across all surfaces, although CLI, MCP, future application UI, and operator diagnostics have different phase/capability bounds (`ux-design-specification.md:83-95`, `:422-436`, `:565-602`; `prd.md:43-57`, `:845-869`).

Fix: define phase-qualified projection profiles that share semantics but include only actions supported on that surface.

#### [Source Drift] Freshness labels lack thresholds and contract-version ownership

`current`, `aging`, `stale`, and `unknown` are presented as stable without thresholds, transitions, recovery, or version binding (`ux-design-specification.md:558-563`, `:1043-1050`; `prd.md:1182-1195`).

Fix: reference the versioned freshness contract, keep ingestion latency separate, and mark unresolved thresholds rather than presenting them as committed.

#### [Source Drift] Activity language implies audit and permission guarantees

“Audit-friendly,” “audit context,” and “permission changed” exceed the current **access telemetry** and membership-metadata contracts (`ux-design-specification.md:252-258`, `:714-743`, `:906-915`; `prd.md:78-93`, `:535-545`).

Fix: use access-telemetry terminology, expose its integrity/retention limitations, and name actual membership or tenant-claim events.

#### [Accessibility] Pointer target sizing is advisory and incomplete

The UX applies a 44px target only to primary actions “where practical,” leaving dense secondary controls and graph interactions unspecified (`ux-design-specification.md:1070-1078`, `:1154-1157`).

Fix: make WCAG 2.2 SC 2.5.8 the binding floor for every author-sized target, retain 44×44 CSS pixels as the preferred high-risk/primary target, and document/test exceptions.

#### [Accessibility] Table, graph, and diagram alternatives are not interaction-complete

The UX asks for textual alternatives but omits grid names/captions, header/sort association, target-qualified row actions, compact-layout parity, and synchronized graph/list selection (`ux-design-specification.md:614-770`, `:851-915`, `:1056-1058`, `:1093-1114`).

Fix: define full grid and graph keyboard/semantic models and pair every Mermaid journey with equivalent numbered text.

#### [Fluent] Historical mock disclaimer is too late

The non-production disclaimer appears only after eight interactive-looking screens, leaving raw controls, bespoke styling, and incomplete ARIA behavior exposed to copy-forward (`ux-design-directions.html:470-478`, `:780-800`).

Fix: put a prominent warning before the first control and next to the UX link; state that spines, FrontComposerShell, the pinned Fluent V5 package, and repository conformance tests win.

### Low (3)

#### [Visual References] No promoted key screen shows the chosen blended direction

The HTML remains eight explorations; no final key screen combines Evidence Cockpit, activity, agent, and operator decisions (`ux-design-specification.md:496-525`; `ux-design-directions.html:470`).

Fix: promote or render only the minimum chosen key screens after the spine contracts are reconciled.

#### [Shape] Legacy frontmatter is workflow metadata, not a current UX contract

The file has workflow-completion fields but no current `name`, `status`, `sources`, or `updated` fields (`ux-design-specification.md:1-16`).

Fix: add complete current frontmatter to both future spines and preserve legacy metadata only as migration provenance.

#### [Fluent] A referenced project context names a stale Fluent release candidate

The referenced Memories context names RC3, while the central catalogue and FrontComposer context select RC5 (`_bmad-output/project-context.md:30`; `references/Hexalith.Builds/Props/Directory.Packages.props:226`).

Fix: refresh the project context or defer exact version resolution to the centrally selected package catalogue.

## Reviewer conclusions

### Rubric walker

The legacy package is broken as a downstream spine contract. Verdicts: flow **broken**, tokens **broken**, components **thin**, states **broken**, visual references **adequate**, bloat **thin**, inheritance **broken**, and shape **broken**. The strongest reusable material is its trust philosophy, progressive disclosure, error recovery, component concepts, and valid Mermaid syntax.

### Source-drift lens

The May UX remains directionally aligned on recoverable trust, graceful degradation, visible tenant/case context, FrontComposer/Fluent inheritance, and future-web accessibility. It is nevertheless unsafe against the change-controlled September PRD until fail-closed tenant semantics, phase/surface ownership, command/schema grammar, RRF/confidence, ingestion, journey, and traceability drift are reconciled.

### Fluent UI V5 / FrontComposer lens

Revise before binding web implementation. The prose establishes the correct high-level boundary, and current Epic 17 specimen code already reduces implementation risk, but the UX must name exact shell/component ownership and the mandatory accordion behavior. The HTML is a historical visual study, not product-code evidence.

### Accessibility lens

Revise before binding active CLI accessibility or activating web. The intent is strong—WCAG 2.2 AA, keyboard trust loop, non-color state, focus return, linear graph narration, restrained live regions, forced colors, reduced motion, and structured MCP recovery—but terminal output, shell inheritance, evidence coverage, and async-state timing need enforceable contracts.

## Mechanical notes

- The legacy UX frontmatter is syntactically valid YAML but not a DESIGN/EXPERIENCE contract.
- `references/Hexalith.Commons/_bmad-output/project-context.md` does not resolve.
- There are no `DESIGN.md`, `EXPERIENCE.md`, `imports/`, `mockups/`, or `wireframes/` artifacts for this legacy contract.
- The five Mermaid `flowchart TD` blocks have balanced fences and no obvious syntax defect.
- No `{path.to.token}` references exist because there is no DESIGN token tree.
- `UX-DR1`–`UX-DR40` are referenced downstream but cannot be resolved in the UX source.
- The source UX and visual HTML were not modified by this validation.

## Reviewer files

- `review-rubric.md`
- `review-source-drift.md`
- `review-fluent-conformance.md`
- `review-accessibility.md`
